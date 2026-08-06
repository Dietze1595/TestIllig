namespace Illig_AI_Platform.Shared.Lieferantenassistent;

public record LieferantenassistentImportBericht(
    int LieferantenGesamt, int LieferantenErsetzt,
    int EmailAdressenGesamt, int EmailAdressenErsetzt,
    int DispositionspositionenGesamt, int DispositionspositionenErsetzt);
