using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein einzelnes erkanntes Merkmal/Sonderoption eines Stücklistenprüfung-Uploads
/// (<see cref="StuecklistenpruefungVerlaufEintrag"/>). Ersetzt die frühere
/// MerkmaleJson/SonderoptionenJson-Speicherung, damit die Sondermerkmalsuche nach
/// Merkmalsnummer über alle Aufträge hinweg filtern kann.
/// </summary>
public class VerlaufMerkmal
{
    public int Id { get; set; }
    public int VerlaufEintragId { get; set; }
    public MerkmalKategorie Kategorie { get; set; }
    public string Position { get; set; } = "";
    public string Merkmalsnummer { get; set; } = "";
    public string Beschreibung { get; set; } = "";
}
