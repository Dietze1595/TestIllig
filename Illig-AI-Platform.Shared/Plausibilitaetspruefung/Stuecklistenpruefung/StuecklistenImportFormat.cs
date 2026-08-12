namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Wählt beim Import die formatspezifischen Parser für <b>beide</b> Dateien — Maximalstückliste
/// (.txt, siehe <see cref="MaximalstuecklisteParser"/>) und Umsetzungsmatrix (.xlsx, siehe
/// <see cref="UmsetzungsmatrixParser"/>). Die Formate sind einander äußerlich sehr ähnlich und
/// lassen sich nicht zuverlässig automatisch unterscheiden — das Format wird beim Import deshalb
/// explizit angegeben. Benannt nach der Leitmaschine, bei der das Format zuerst auftrat; dasselbe
/// Format gilt in der Regel für weitere Maschinen. Übergangslösung, bis ILLIG standardisierte
/// Export-/Matrix-Dateien liefert.
/// </summary>
public enum StuecklistenImportFormat
{
    /// <summary>Ursprüngliches Format (Leitmaschine RDM 75Kc): Kurztext/Menge/Einheit in Spalte 17/18/22.</summary>
    Rdm75Kc,

    /// <summary>Format der Leitmaschine RDM 73K: Wert-Spalten um zwei verschoben (19/20/24).</summary>
    Rdm73k,

    /// <summary>
    /// Format der Leitmaschine RDM 76Kb: .txt wie RDM75 (17/18/22), aber .xlsx eigen —
    /// Varianten ab Spalte 13, Kopf in Zeile 3, durchgestrichene Alt-Nummern in der Hierarchie.
    /// </summary>
    Rdm76Kb,

    /// <summary>
    /// Eigenständiges RDK80k-Format: .txt mit Wert-Spalten 19/20/24 und .xlsx mit
    /// RDK80-/RDKP72-Abschnitten sowie Hierarchie in L–Q.
    /// </summary>
    Rdk80k
}
