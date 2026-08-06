namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Ergebnis des LLM-gestützten Volltextvergleichs zwischen Angebot und Kundenbestellung.
/// Entscheidungshilfe für den Innendienst, keine automatische Freigabe/Ablehnung
/// (siehe Spec Update 2026-07-16).
/// </summary>
public record AngebotsVergleichLlmErgebnis(
    string? LieferterminAngebot,
    string? LieferterminBestaetigung,
    bool LieferterminIdentisch,
    IReadOnlyList<string> Uebereinstimmungen,
    IReadOnlyList<string> SonstigeAbweichungen,
    bool IstBestelldokument = true,
    string? DokumentartHinweis = null);
