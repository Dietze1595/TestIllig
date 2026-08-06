namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Client-eigenes Spiegel-Enum zum Server-<c>StuecklistenImportFormat</c> (der WASM-Client darf
/// Shared nicht referenzieren, vgl. übrige Client-DTOs). Die Namen MÜSSEN mit dem Server-Enum
/// übereinstimmen — sie werden als Wert des Formularfelds „format" gesendet und dort geparst.
/// Benannt nach der Leitmaschine, für die das Format zuerst auftrat; dasselbe Format gilt in der
/// Regel für weitere Maschinen. Übergangslösung, bis ILLIG standardisierte Dateien liefert.
/// </summary>
public enum StuecklistenImportFormat
{
    Rdm75Kc,
    Rdm73k,
    Rdm76Kb
}

/// <summary>Anzeigetexte für die Format-Auswahl im Import-UI.</summary>
public static class StuecklistenImportFormatInfo
{
    public static string Bezeichnung(StuecklistenImportFormat format) => format switch
    {
        StuecklistenImportFormat.Rdm75Kc => "RDM 75Kc",
        StuecklistenImportFormat.Rdm73k => "RDM 73K",
        StuecklistenImportFormat.Rdm76Kb => "RDM 76Kb",
        _ => format.ToString()
    };
}
