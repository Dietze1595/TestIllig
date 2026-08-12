namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Trägt zusätzlich zum unveränderlichen <see cref="VergleichsKnoten"/> den Klapp-Zustand
/// pro Knoten für die Baum-Ansicht (<see cref="VergleichsBaumZeile"/>).
/// </summary>
public sealed class VergleichsKnotenAnsicht
{
    public required VergleichsKnoten Knoten { get; init; }
    public bool Aufgeklappt { get; set; }
    public required IReadOnlyList<VergleichsKnotenAnsicht> Kinder { get; init; }

    public bool EnthaeltAbweichung =>
        Knoten.Status is VergleichsStatus.Abweichung or VergleichsStatus.NurBeiUns
        || Kinder.Any(kind => kind.EnthaeltAbweichung);

    // Verallgemeinerung von EnthaeltAbweichung auf eine beliebige Statusauswahl: true, wenn
    // dieser Knoten oder irgendein Nachfahre einen Status aus der Menge trägt. Grundlage für den
    // Status-Spaltenfilter (VergleichsErgebnisTabelle) — hält den Pfad zu Treffern sichtbar.
    public bool EnthaeltStatus(IReadOnlySet<VergleichsStatus> status) =>
        status.Contains(Knoten.Status) || Kinder.Any(kind => kind.EnthaeltStatus(status));

    // Wie EnthaeltStatus, aber für die Textsuche: true, wenn dieser Knoten oder ein Nachfahre die
    // gesuchte Zeichenfolge in Artikelnummer oder Bezeichnung trägt — hält den Pfad zu Treffern sichtbar.
    public bool EnthaeltText(string suchtext) =>
        Knoten.Artikelnummer.Contains(suchtext, StringComparison.OrdinalIgnoreCase)
        || Knoten.Bezeichnung.Contains(suchtext, StringComparison.OrdinalIgnoreCase)
        || Kinder.Any(kind => kind.EnthaeltText(suchtext));

    // Default-Klappzustand (bottom-up): ein Knoten wird aufgeklappt, um darunterliegende
    // Probleme sichtbar zu machen — nämlich wenn ein Kind selbst ein Problem trägt (Status
    // ungleich Übereinstimmung) oder seinerseits aufgeklappt ist (tieferliegendes Problem).
    //
    // Ausnahme: ein fehlender Knoten (NurBeiUns) bleibt eingeklappt. Sein Status gilt per
    // Konstruktion für den gesamten Teilbaum (alle Nachfahren sind ebenfalls NurBeiUns) — es
    // genügt, den obersten fehlenden Knoten zu zeigen; die Unterkomponenten erscheinen erst
    // beim manuellen Aufklappen. Die Eltern klappen trotzdem auf, weil das fehlende Kind ein
    // Problem trägt und so sichtbar wird.
    public static VergleichsKnotenAnsicht Aufbauen(VergleichsKnoten knoten)
    {
        var kinder = knoten.Kinder.Select(Aufbauen).ToList();
        var hatZeigenswertesKind = kinder.Any(kind =>
            kind.Knoten.Status != VergleichsStatus.Uebereinstimmung || kind.Aufgeklappt);
        var aufgeklappt = knoten.Status != VergleichsStatus.NurBeiUns && hatZeigenswertesKind;
        return new VergleichsKnotenAnsicht
        {
            Knoten = knoten,
            Aufgeklappt = aufgeklappt,
            Kinder = kinder,
        };
    }

    // Flacht den sichtbaren Teil des Baums zu einer Liste ab — Grundlage für die
    // virtualisierte Anzeige (VergleichsErgebnisTabelle). Reine Objekt-Rekursion (billig),
    // nicht zu verwechseln mit der früheren Komponenten-Rekursion (teuer). Bildet exakt
    // die bisherige Sichtbarkeits-/Aufklapp-Logik nach: ein Knoten ist enthalten, wenn der
    // Filter ihn durchlässt; seine Kinder folgen nur, wenn er selbst aufgeklappt ist (oder
    // alleSichtbarenKinderAnzeigen das erzwingt).
    public static IEnumerable<VergleichsZeileAnsicht> SichtbareZeilen(
        VergleichsKnotenAnsicht knoten, int tiefe,
        Func<VergleichsKnotenAnsicht, bool>? filter, bool alleSichtbarenKinderAnzeigen)
    {
        if (!(filter?.Invoke(knoten) ?? true))
            yield break;

        var hatSichtbareKinder = knoten.Kinder.Any(k => filter?.Invoke(k) ?? true);
        var istAufgeklappt = alleSichtbarenKinderAnzeigen || knoten.Aufgeklappt;

        yield return new VergleichsZeileAnsicht(knoten, tiefe, hatSichtbareKinder, istAufgeklappt);

        if (!istAufgeklappt)
            yield break;

        foreach (var kind in knoten.Kinder)
            foreach (var zeile in SichtbareZeilen(kind, tiefe + 1, filter, alleSichtbarenKinderAnzeigen))
                yield return zeile;
    }
}
