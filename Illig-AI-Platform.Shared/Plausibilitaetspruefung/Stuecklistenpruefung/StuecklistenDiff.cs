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
        // Gleiche Stelle, andere Artikelnummer = ersetztes Teil (per Bezeichnung gepaart, s. u.).
        // Wichtigster Hinweis, daher zuerst.
        if (!string.Equals(unser.Artikelnummer, sap.Artikelnummer, StringComparison.OrdinalIgnoreCase))
            hinweise.Add($"Anderes Teil: erwartet {unser.Artikelnummer}, in SAP {sap.Artikelnummer}");
        if (unser.Menge != sap.Menge)
            hinweise.Add($"Menge {unser.Menge} statt {sap.Menge}");
        if (!string.Equals(unser.Einheit, sap.Einheit, StringComparison.OrdinalIgnoreCase))
            hinweise.Add($"Einheit {unser.Einheit} statt {sap.Einheit}");
        if (!string.Equals(unser.Bezeichnung, sap.Bezeichnung, StringComparison.OrdinalIgnoreCase))
            hinweise.Add($"Bezeichnung unterschiedlich - \"{sap.Bezeichnung}\"");

        var sapKinderNachArtikel = VereinigeGeschwister(sap.Kinder);
        var partner = OrdneKinderZu(unser.Kinder, sapKinderNachArtikel, out var uebrigeSapKinder);
        var kinder = new List<VergleichsKnoten>();

        for (var i = 0; i < unser.Kinder.Count; i++)
            kinder.Add(VergleicheKnoten(unser.Kinder[i], partner[i], pfad, nurInSap));

        foreach (var sapKind in uebrigeSapKinder)
            SammleNurInSap(sapKind, pfad, nurInSap);

        return new VergleichsKnoten(
            unser.Artikelnummer, unser.Bezeichnung,
            unser.Menge, unser.Einheit, sap.Menge, sap.Einheit,
            hinweise.Count == 0 ? VergleichsStatus.Uebereinstimmung : VergleichsStatus.Abweichung,
            hinweise.Count == 0 ? null : string.Join("; ", hinweise),
            kinder);
    }

    /// <summary>
    /// Ordnet jedem unserer Kind-Knoten (nach Reihenfolge) einen SAP-Partner zu: zuerst exakt über
    /// die Artikelnummer, danach übrig gebliebene Kinder über Bezeichnungs-Ähnlichkeit (gleiches
    /// führendes Wort, z. B. "Gehäuse…"). So wird ein an gleicher Stelle verbautes, aber anders
    /// nummeriertes Ersatzteil als Abweichung erkannt statt als "fehlt hier / nur in SAP".
    /// Rückgabe: Partner-Array in der Reihenfolge von <paramref name="unsereKinder"/> (null =
    /// kein Partner); <paramref name="uebrigeSapKinder"/> = SAP-Kinder ohne Partner.
    /// </summary>
    private static RohPosition?[] OrdneKinderZu(
        IReadOnlyList<StuecklistenKnoten> unsereKinder,
        Dictionary<string, RohPosition> sapKinderNachArtikel,
        out List<RohPosition> uebrigeSapKinder)
    {
        var partner = new RohPosition?[unsereKinder.Count];
        var verwendet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Exakt über die Artikelnummer.
        for (var i = 0; i < unsereKinder.Count; i++)
        {
            if (sapKinderNachArtikel.TryGetValue(unsereKinder[i].Artikelnummer, out var sapKind))
            {
                partner[i] = sapKind;
                verwendet.Add(unsereKinder[i].Artikelnummer);
            }
        }

        var uebrig = sapKinderNachArtikel
            .Where(kv => !verwendet.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        // 2) Rest über Bezeichnungs-Ähnlichkeit paaren (bester gemeinsamer Präfix gewinnt).
        for (var i = 0; i < unsereKinder.Count && uebrig.Count > 0; i++)
        {
            if (partner[i] is not null)
                continue;

            var kandidat = uebrig
                .Where(s => SindVerwandt(unsereKinder[i].Bezeichnung, s.Bezeichnung))
                .OrderByDescending(s => GemeinsamePraefixLaenge(unsereKinder[i].Bezeichnung, s.Bezeichnung))
                .FirstOrDefault();
            if (kandidat is not null)
            {
                partner[i] = kandidat;
                uebrig.Remove(kandidat);
            }
        }

        uebrigeSapKinder = uebrig;
        return partner;
    }

    // Zwei Teile gelten als "gleicher Slot", wenn ihre Bezeichnung dasselbe führende Wort hat
    // (z. B. "Gehäuse_RDM75K/76K" und "Gehäuse_RDM75Kc_RDML75b"). Mindestlänge 3 verhindert
    // Paarungen über sehr kurze Fragmente.
    private static bool SindVerwandt(string bezeichnungA, string bezeichnungB) =>
        FuehrenderBegriff(bezeichnungA) is { Length: >= 3 } begriff
        && string.Equals(begriff, FuehrenderBegriff(bezeichnungB), StringComparison.OrdinalIgnoreCase);

    private static string FuehrenderBegriff(string bezeichnung) =>
        bezeichnung.Split(['_', '/', ' ', '-'], StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } teile
            ? teile[0]
            : bezeichnung.Trim();

    private static int GemeinsamePraefixLaenge(string a, string b)
    {
        var laenge = 0;
        while (laenge < a.Length && laenge < b.Length
               && char.ToUpperInvariant(a[laenge]) == char.ToUpperInvariant(b[laenge]))
            laenge++;
        return laenge;
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
