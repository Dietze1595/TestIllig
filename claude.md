# Anweisung für Claude: Nur Frontend-Anpassungen

Diese Datei gilt für Sessions, in denen eine **Frontend-Designerin (keine Entwicklerin)**
mit Claude an diesem Projekt arbeitet. Sie ist keine Entwicklerin und soll **nicht am
Backend oder an der Datenbank** arbeiten, sondern ausschließlich die Oberfläche optisch
schöner machen.

Ziel ist ausschließlich:

- **Optisches Feintuning** der Oberfläche: Farben, Abstände, Schriften, Layout, Icons,
  Responsive-Verhalten.
- **Rein visuelle Themen und Anzeige-/Filterungen im Frontend** (z. B. Sortierung, Suche
  oder Filter-Bedienelemente auf bereits vom Backend gelieferten Daten – **clientseitig**,
  ohne neue API-Aufrufe oder Backend-Logik).

**Keine Backend- oder Datenbank-Änderungen.** Wenn eine Aufgabe nach Backend, API oder
Datenbank aussieht, setzt Claude sie nicht um, sondern erklärt kurz, dass das mit dem
Entwickler-Team abgestimmt werden muss (siehe unten).

## Erlaubt (Frontend)

Änderungen sind ausschließlich in diesem Ordner erlaubt:

- `Illig-AI-Platform.Client/` – die komplette Blazor-WASM-Oberfläche
  - `.razor` / `.razor.cs` (Markup, Styling, Layout sowie clientseitige Anzeige-/
    Filterlogik auf bereits vorhandenen Daten – **keine neue Business-Logik**)
  - `wwwroot/` (CSS, Bilder, Icons, Fonts, `index.html`)
  - `Layout/` (z. B. `MainLayout.razor`)
  - `Components/`, `Pages/`

Der Designguide mit den Projektfarben steht in [`agents.md`](agents.md) – bitte daran halten
(Primary #00457b / #9bc832, siehe dort für Sekundärfarben).

## Bestehende Komponenten wiederverwenden

Bevor Claude eine Frontend-Anpassung umsetzt, **prüft Claude zuerst, ob es für das
betroffene UI-Element bereits eine Komponente gibt** (z. B. in `Components/`,
`Layout/` oder als wiederverwendetes Markup in anderen `.razor`-Seiten).

- Ist eine passende Komponente vorhanden, **wird wenn möglich diese Komponente
  angepasst**, statt eine neue zu erstellen oder das gewünschte Aussehen direkt
  in der einzelnen Seite/Page nachzubauen.
- Nur wenn keine passende Komponente existiert, darf Claude die Änderung direkt
  in der jeweiligen `.razor`-Datei umsetzen.
- Bei Unsicherheit, ob eine bestehende Komponente die richtige Stelle für die
  Änderung ist (z. B. weil sie an mehreren Stellen verwendet wird und die
  Änderung dort überall Auswirkungen hätte), lieber kurz nachfragen.

## Nicht erlaubt (Backend – niemals anfassen)

- `Illig-AI-Platform/` (die API / der Server, Controller, `Program.cs`, `appsettings*.json`)
- `Illig-AI-Platform.Shared/` (Datenbank-Zugriff, Entity-Framework-Modelle, Business-Logik)
- `Illig-AI-Platform.Tests/`
- Datenbank, Migrationen, Connection Strings, Secrets, API-Keys
- `.csproj`-Dateien, NuGet-Pakete, `azure-pipelines.yml`

Falls eine gewünschte Design-Änderung eigentlich eine Backend-Änderung erfordern würde
(z. B. neue Felder aus der Datenbank, geänderte API-Antworten), **soll Claude das nicht
selbst umsetzen**, sondern kurz erklären, dass dieser Wunsch Rückstimmung mit dem
Entwickler-Team benötigt.

## Weitere Leitplanken

- Keine Terminal-Befehle außer dem lokalen Start des Frontends zum Testen im Browser
  (z. B. `dotnet run` im Client-Projekt bzw. den vorhandenen Dev-Server).
- **Claude committet die Änderungen selbst** (mit einer passenden Commit-Nachricht),
  nachdem sie abgeschlossen und im Browser getestet wurden. **Gepusht wird nur
  von der Designerin selbst.**
- Keine Dateien außerhalb des Client-Ordners löschen oder umbenennen.
- Alle Änderungen werden direkt auf dem `master`-Branch erstellt und committet –
  **kein Worktree, kein separater Feature-Branch**.
- Bei Unsicherheit, ob etwas "nur Design" oder schon "Logik" ist: lieber nachfragen statt
  einfach umsetzen.
- Änderungen immer im Browser testen und kurz zeigen/beschreiben, was sich optisch geändert
  hat.
