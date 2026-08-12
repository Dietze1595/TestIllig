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

        var normalisierterMaschinentyp = Normalisieren(maschinentyp);
        var fuerMaschinentypVorhanden =
            !string.IsNullOrWhiteSpace(normalisierterMaschinentyp)
            && vorhandeneSchluessel.Any(
                schluessel => normalisierterMaschinentyp.StartsWith(
                    Normalisieren(schluessel),
                    StringComparison.OrdinalIgnoreCase));

        return new UmsetzungsmatrixVerfuegbarkeit(
            fuerMaschinentypVorhanden,
            vorhandeneSchluessel);
    }

    public async Task<StuecklistenKnoten?> AufbauenAsync(string maschinentyp, IReadOnlyCollection<string> merkmalsnummern)
    {
        var normalisierterMaschinentyp = Normalisieren(maschinentyp);
        var stuecklisten = await db.MaschinentypStuecklisten
            .AsNoTracking()
            .ToListAsync();
        var stueckliste = stuecklisten
            .Where(m => normalisierterMaschinentyp.StartsWith(
                Normalisieren(m.MaschinentypSchluessel),
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.MaschinentypSchluessel.Length) // längster/spezifischster Treffer gewinnt
            .FirstOrDefault();
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
        return BauenRekursiv(wurzel, kinderNachParent, merkmalsnummern, stueckliste.MaschinentypSchluessel);
    }

    private static StuecklistenKnoten? BauenRekursiv(
        MaximalstuecklistenPosition position, Dictionary<int, List<MaximalstuecklistenPosition>> kinderNachParent,
        IReadOnlyCollection<string> merkmalsnummern, string maschinentypSchluessel)
    {
        if (position.Bedingung is not null
            && !UmsetzungsmatrixBedingung.Parse(position.Bedingung).Erfuellt(merkmalsnummern))
            return null;

        // Fremde Grundmaschinen-Varianten (dasselbe Teil für eine andere Grundmaschine, ohne
        // trennende Matrix-Bedingung) vor dem Abstieg aussortieren — siehe GrundmaschinenVariantenFilter.
        var alleKinder = kinderNachParent.GetValueOrDefault(position.Id, []);
        var fremdeVarianten = GrundmaschinenVariantenFilter.AuszuschliessendeVarianten(alleKinder, maschinentypSchluessel);
        var kinder = alleKinder
            .Where(k => !fremdeVarianten.Contains(k))
            .Select(k => BauenRekursiv(k, kinderNachParent, merkmalsnummern, maschinentypSchluessel))
            .Where(k => k is not null)
            .Select(k => k!)
            .ToList();

        return new StuecklistenKnoten(position.Artikelnummer, position.Bezeichnung, position.Menge, position.Einheit, kinder);
    }

    private static string Normalisieren(string? wert) =>
        string.Concat((wert ?? "").Where(zeichen => !char.IsWhiteSpace(zeichen)));
}
