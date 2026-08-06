namespace Illig_AI_Platform.Shared.Lieferantenassistent;

public enum LieferterminStatus { ImPlan, BaldFaellig, Ueberfaellig }

public static class LieferterminStatusBerechnung
{
    private const int BaldFaelligTage = 7;

    public static LieferterminStatus Berechnen(DateOnly lieferdatum, DateOnly heute)
    {
        if (lieferdatum < heute)
            return LieferterminStatus.Ueberfaellig;
        if (lieferdatum <= heute.AddDays(BaldFaelligTage))
            return LieferterminStatus.BaldFaellig;
        return LieferterminStatus.ImPlan;
    }
}
