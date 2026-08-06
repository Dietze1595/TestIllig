using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenAufbauService(AppDbContext db)
{
    public async Task<bool> IstUmsetzungsmatrixVorhandenAsync(
        string maschinentyp,
        CancellationToken cancellationToken = default)
    {
        var verfuegbarkeit = await UmsetzungsmatrixVerfuegbarkeitAsync(
            maschinentyp,
            cancellationToken);
        return verfuegbarkeit.FuerMaschinentypVorhanden;
    }

    public async Task<UmsetzungsmatrixVerfuegbarkeit> UmsetzungsmatrixVerfuegbarkeitAsync(
        string maschinentyp,
        CancellationToken cancellationToken = default)
    {
        var vorhandeneSchluessel = await db.MaschinentypStuecklisten
            .AsNoTracking()
            .OrderBy(m => m.MaschinentypSchluessel)
            .Select(m => m.MaschinentypSchluessel)
            .ToListAsync(cancellationToken);

        var fuerMaschinentypVorhanden =
            !string.IsNullOrWhiteSpace(maschinentyp)
            && vorhandeneSchluessel.Any(
                schluessel => maschinentyp.StartsWith(
                    schluessel,
                    StringComparison.OrdinalIgnoreCase));

        return new UmsetzungsmatrixVerfuegbarkeit(
            fuerMaschinentypVorhanden,
            vorhandeneSchluessel);
    }

    public async Task<StuecklistenKnoten?> AufbauenAsync(string maschinentyp, IReadOnlyCollection<string> merkmalsnummern)
    {
        var stueckliste = await db.MaschinentypStuecklisten
            .AsNoTracking()
            .Where(m => maschinentyp.StartsWith(m.MaschinentypSchluessel))
            .OrderByDescending(m => m.MaschinentypSchluessel.Length) // längster/spezifischster Treffer gewinnt
            .FirstOrDefaultAsync();
        if (stueckliste is null)
            return null;

        var positionen = await db.MaximalstuecklistenPositionen
            .AsNoTracking()
            .Where(p => p.MaschinentypStuecklisteId == stueckliste.Id)
            .ToListAsync();

        var kinderNachParent = positionen
            .Where(p => p.ParentId is not null)
            .GroupBy(p => p.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Reihenfolge).ToList());

        var wurzel = positionen.Single(p => p.ParentId is null);
        return BauenRekursiv(wurzel, kinderNachParent, merkmalsnummern);
    }

    private static StuecklistenKnoten? BauenRekursiv(
        MaximalstuecklistenPosition position, Dictionary<int, List<MaximalstuecklistenPosition>> kinderNachParent,
        IReadOnlyCollection<string> merkmalsnummern)
    {
        if (position.Bedingung is not null
            && !UmsetzungsmatrixBedingung.Parse(position.Bedingung).Erfuellt(merkmalsnummern))
            return null;

        var kinder = kinderNachParent.GetValueOrDefault(position.Id, [])
            .Select(k => BauenRekursiv(k, kinderNachParent, merkmalsnummern))
            .Where(k => k is not null)
            .Select(k => k!)
            .ToList();

        return new StuecklistenKnoten(position.Artikelnummer, position.Bezeichnung, position.Menge, position.Einheit, kinder);
    }
}
