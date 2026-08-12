using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Grenzt grundmaschinen-spezifische Varianten ab, die die Umsetzungsmatrix nicht per Bedingung
/// trennt. Hintergrund: In der Maximalstückliste liegen unter demselben Elternknoten teils zwei
/// bedingungslose Basis-Positionen, die je zu einer anderen Grundmaschine gehören (z. B.
/// „Kabel_Basis_RDM54-76K/Kc_Untertisch" 9281770 vs. „Kabel_Basis_RDM75K/Kc_Untertisch" 9308223).
/// Diese Zugehörigkeit steht nur im SAP-Beziehungswissen, nicht in der Matrix — der generische
/// „keine Bedingung = immer enthalten"-Aufbau würde daher beide aufnehmen und die fremde Variante
/// fälschlich als „Fehlt in Stückliste" melden.
///
/// Die Heuristik arbeitet bewusst eng, um legitime Positionen nicht zu verlieren: Es konkurrieren
/// nur Geschwister, deren Bezeichnung <b>nach Entfernen der RDM/RDK-Kennung identisch</b> ist (also
/// dasselbe Teil in mehreren Grundmaschinen-Ausprägungen). Nur wenn eine dieser Varianten den
/// Maschinentyp exakt nennt, werden die übrigen (fremden) ausgeschlossen. Positionen mit Bedingung
/// bleiben unangetastet (dafür ist die Bedingungslogik zuständig).
/// </summary>
public static partial class GrundmaschinenVariantenFilter
{
    // RDM/RDK-Maschinenkennung im Bezeichnungstext, inkl. Bereich (54-76), Größenbuchstaben (K/Kc)
    // und angehängter Zweitgröße (…/76K). Beispiele: "RDM54-76K/Kc", "RDM75K/Kc", "RDM75Kc/76K".
    [GeneratedRegex(@"RD[MK]\s*\d{2,3}(?:[-–]\d{2,3})*[A-Za-z]*(?:/[A-Za-z0-9]+)*", RegexOptions.IgnoreCase)]
    private static partial Regex Kennung();

    [GeneratedRegex(@"RD[MK]\s*(\d{2,3})", RegexOptions.IgnoreCase)]
    private static partial Regex MaschinenNummerAusSchluessel();

    [GeneratedRegex(@"\d{2,3}")]
    private static partial Regex Zahl();

    [GeneratedRegex(@"[_\s/]+")]
    private static partial Regex TrennzeichenFolge();

    public static IReadOnlySet<MaximalstuecklistenPosition> AuszuschliessendeVarianten(
        IReadOnlyCollection<MaximalstuecklistenPosition> geschwister,
        string maschinentypSchluessel)
    {
        var ergebnis = new HashSet<MaximalstuecklistenPosition>();

        var zielTreffer = MaschinenNummerAusSchluessel().Match(maschinentypSchluessel ?? "");
        if (!zielTreffer.Success)
            return ergebnis;
        var zielNummer = zielTreffer.Groups[1].Value;

        // Kandidaten: bedingungslose Geschwister mit erkennbarer Grundmaschinen-Kennung.
        var kandidaten = geschwister
            .Where(p => p.Bedingung is null)
            .Select(p => (Position: p, Kennung: Kennung().Match(p.Bezeichnung ?? "")))
            .Where(x => x.Kennung.Success)
            .Select(x => (
                x.Position,
                Normalisiert: OhneKennung(x.Position.Bezeichnung ?? "", x.Kennung.Value),
                Nummern: Nummern(x.Kennung.Value)))
            .ToList();

        // Gruppieren nach "gleichem Teil" (Bezeichnung ohne Maschinenkennung). Nur echte Varianten
        // voneinander konkurrieren; unterschiedliche Funktionsgruppen bleiben unberührt.
        foreach (var gruppe in kandidaten.GroupBy(k => k.Normalisiert))
        {
            var mitglieder = gruppe.ToList();
            if (mitglieder.Count < 2)
                continue;

            // Nur ausschließen, wenn eine Variante den Maschinentyp exakt nennt — sonst nicht raten
            // (Bereichsnamen wie "RDM54-76K" schließen 75 sonst mit ein und blieben erhalten).
            if (!mitglieder.Any(m => m.Nummern.Contains(zielNummer)))
                continue;

            foreach (var m in mitglieder.Where(m => !m.Nummern.Contains(zielNummer)))
                ergebnis.Add(m.Position);
        }

        return ergebnis;
    }

    private static string OhneKennung(string bezeichnung, string kennung)
    {
        var rest = bezeichnung.Replace(kennung, "", StringComparison.OrdinalIgnoreCase);
        // Trennzeichen zusammenfassen, damit "Kabel_Basis__Untertisch" == "Kabel_Basis_Untertisch".
        return TrennzeichenFolge().Replace(rest, "_").Trim('_').ToLowerInvariant();
    }

    private static HashSet<string> Nummern(string kennung) =>
        Zahl().Matches(kennung).Select(m => m.Value).ToHashSet();
}
