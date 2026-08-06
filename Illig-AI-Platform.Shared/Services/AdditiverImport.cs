using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Services;

/// <summary>
/// Gemeinsames Muster für alle SAP-Datenanlieferungen: Der einmalige Bulk-Push
/// und die täglichen Delta-Pushes laufen über denselben Endpunkt, der Import
/// arbeitet additiv pro fachlichem Schlüssel (z. B. Auftragsnummer oder
/// Kundennummer). Nur Datensätze, deren Schlüssel im Push enthalten ist,
/// werden ersetzt — Datensätze anderer Schlüssel bleiben unangetastet,
/// es wird nie global gelöscht.
/// </summary>
public static class AdditiverImport
{
    /// <summary>
    /// Ersetzt alle vorhandenen Zeilen, deren Schlüssel in <paramref name="neueZeilen"/>
    /// vorkommt, durch die neuen Zeilen und fügt Zeilen neuer Schlüssel hinzu.
    /// </summary>
    /// <returns>Anzahl der ersetzten (vorher vorhandenen) Zeilen.</returns>
    public static async Task<int> ErsetzeProSchluesselAsync<TEntity, TSchluessel>(
        DbContext db,
        IReadOnlyList<TEntity> neueZeilen,
        Expression<Func<TEntity, TSchluessel>> schluessel)
        where TEntity : class
    {
        var schluesselWert = schluessel.Compile();
        var schluesselWerte = neueZeilen.Select(schluesselWert).Distinct().ToList();

        // e => schluesselWerte.Contains(schluessel(e)) — als Expression, damit EF
        // die Abfrage in SQL übersetzen kann statt die ganze Tabelle zu laden.
        var vorhandenPredicate = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(TSchluessel)],
                Expression.Constant(schluesselWerte),
                schluessel.Body),
            schluessel.Parameters[0]);

        var set = db.Set<TEntity>();
        var vorhandeneZeilen = await set.Where(vorhandenPredicate).ToListAsync();
        set.RemoveRange(vorhandeneZeilen);
        set.AddRange(neueZeilen);
        await db.SaveChangesAsync();

        return vorhandeneZeilen.Count;
    }
}
