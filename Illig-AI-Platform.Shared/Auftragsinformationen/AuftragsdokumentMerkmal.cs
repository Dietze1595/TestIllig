using Illig_AI_Platform.Shared.Plausibilitaetspruefung;

namespace Illig_AI_Platform.Shared.Auftragsinformationen;

public class AuftragsdokumentMerkmal
{
    public int Id { get; set; }
    public int AuftragsdokumentId { get; set; }
    public MerkmalKategorie Kategorie { get; set; }
    public string Position { get; set; } = "";
    public string Merkmalsnummer { get; set; } = "";
    public string Beschreibung { get; set; } = "";
}
