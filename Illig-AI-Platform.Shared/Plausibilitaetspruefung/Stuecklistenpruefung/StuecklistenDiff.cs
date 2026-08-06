namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public static class StuecklistenDiff
{
    public static VergleichsErgebnis Vergleiche(StuecklistenKnoten unsereWurzel, RohPosition sapWurzel)
    {
        var nurInSap = new List<VergleichsPosition>();
        var wurzel = VergleicheKnoten(unsereWurzel, sapWurzel, "", nurInSap);
        return new VergleichsErgebnis(wurzel, nurInSap);
    }

    private static VergleichsKnoten VergleicheKnoten(
        StuecklistenKnoten unser, RohPosition? sap, string elternPfad,
        List<VergleichsPosition> nurInSap)
    {
        var pfad = string.IsNullOrEmpty(elternPfad) ? unser.Artikelnummer : $"{elternPfad} › {unser.Artikelnummer}";

        if (sap is null)
        {
            var kinderOhneSap = unser.Kinder
                .Select(kind => VergleicheKnoten(kind, null, pfad, nurInSap))
                .ToList();
            return new VergleichsKnoten(
                unser.Artikelnummer, unser.Bezeichnung,
                unser.Menge, unser.Einheit, null, null,
                VergleichsStatus.NurBeiUns, null, kinderOhneSap);
        }

        var hinweise = new List<string>();
        if (unser.Menge != sap.Menge)
            hinweise.Add($"Menge {unser.Menge} statt {sap.Menge}");
        if (!string.Equals(unser.Einheit, sap.Einheit, StringComparison.OrdinalIgnoreCase))
            hinweise.Add($"Einheit {unser.Einheit} statt {sap.Einheit}");
        if (!string.Equals(unser.Bezeichnung, sap.Bezeichnung, StringComparison.OrdinalIgnoreCase))
            hinweise.Add($"Bezeichnung unterschiedlich - \"{sap.Bezeichnung}\"");

        var sapKinderNachArtikel = VereinigeGeschwister(sap.Kinder);
        var zugeordnet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kinder = new List<VergleichsKnoten>();

        foreach (var unserKind in unser.Kinder)
        {
            sapKinderNachArtikel.TryGetValue(unserKind.Artikelnummer, out var sapKind);
            if (sapKind is not null)
                zugeordnet.Add(unserKind.Artikelnummer);
            kinder.Add(VergleicheKnoten(unserKind, sapKind, pfad, nurInSap));
        }

        foreach (var (artikelnummer, sapKind) in sapKinderNachArtikel)
        {
            if (zugeordnet.Contains(artikelnummer))
                continue;
            SammleNurInSap(sapKind, pfad, nurInSap);
        }

        return new VergleichsKnoten(
            unser.Artikelnummer, unser.Bezeichnung,
            unser.Menge, unser.Einheit, sap.Menge, sap.Einheit,
            hinweise.Count == 0 ? VergleichsStatus.Uebereinstimmung : VergleichsStatus.Abweichung,
            hinweise.Count == 0 ? null : string.Join("; ", hinweise),
            kinder);
    }

    private static Dictionary<string, RohPosition> VereinigeGeschwister(List<RohPosition> kinder) =>
        kinder
            .GroupBy(k => k.Artikelnummer, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Count() == 1 ? g.First() : g.First() with { Menge = g.Sum(k => k.Menge) },
                StringComparer.OrdinalIgnoreCase);

    private static void SammleNurInSap(RohPosition sap, string elternPfad, List<VergleichsPosition> nurInSap)
    {
        var pfad = $"{elternPfad} › {sap.Artikelnummer}";
        nurInSap.Add(new VergleichsPosition(
            pfad, sap.Artikelnummer, sap.Bezeichnung,
            null, null, sap.Menge, sap.Einheit, VergleichsStatus.NurInSap, null));
        foreach (var kind in sap.Kinder)
            SammleNurInSap(kind, pfad, nurInSap);
    }
}
