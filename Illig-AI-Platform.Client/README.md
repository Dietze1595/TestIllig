# Illig-AI-Platform.Client

Blazor WebAssembly-Frontend der ILLIG KI-Plattform. Dieses Dokument richtet sich an alle, die
hier UI/Design-Arbeit machen, ohne den Rest der Lösung (Backend-API, Datenbank) im Detail zu
kennen.

## Lokal starten

Der Client wird **nicht eigenständig** gestartet — er wird vom Backend-Projekt mitgehostet:

```
dotnet run --project ../Illig-AI-Platform
```

Das startet die API unter `http://localhost:5081` bzw. `https://localhost:7081` und liefert dort
auch die Blazor-App aus (`app.UseBlazorFrameworkFiles()` in `Illig-AI-Platform/Program.cs`). Ein
eigener `dotnet run` im Client-Ordner erzeugt keine funktionierende Umgebung, da API-Aufrufe ins
Leere gehen würden.

**Wichtig für externe Teams ohne Zugriff auf Datenbank/Azure Key Vault:** Ohne DB-Verbindung und
B2C-Konfiguration lädt die App zwar, aber Login und alle Datenabfragen schlagen fehl. Für reine
Markup-/CSS-Arbeit ist das meist kein Problem (Seiten lassen sich trotzdem im Quellcode lesen und
per statischem Rendering im Browser-Devtools begutachten); für ein voll funktionsfähiges Preview
mit echten Daten braucht es entweder eine eigene lokale DB + `Db:ConnectionString` in
`Illig-AI-Platform/appsettings.Development.json`, oder eine von uns bereitgestellte Umgebungs-URL.

## Projektstruktur

Pages, Components, Services und Models sind jeweils **pro Use Case** in Unterordnern organisiert:

```
Pages/
  Auftragsanlage/
  Lieferantenassistent/
  Plausibilitaetspruefung/
    Sondermerkmalsuche/
    Stuecklistenpruefung/
  Home.razor, Profile.razor, ...        ← global (kein Use Case)
Components/
  Plausibilitaetspruefung/Stuecklistenpruefung/   ← use-case-spezifische Komponenten
  ApiFehlerHinweis.razor, Modal.razor, ...          ← global (mehrere Use Cases nutzen sie)
Services/  Models/                       ← analog aufgeteilt
```

Faustregel: Wird eine Datei nur von einem Use Case gebraucht, gehört sie in dessen Unterordner.
Wird sie von mehreren gebraucht (z. B. `ApiFehlerHinweis`, `Modal`, `FileDropzone`,
`ChecklistItem`, `StepIndicator`), bleibt sie auf der jeweiligen Root-Ebene (`Components/`,
`Services/`, `Models/`).

`AppRoutes.cs` (Projekt-Root) bündelt alle Routen-Pfade an einer Stelle — Links, `NavigateTo(...)`
und der Breadcrumb in `Layout/MainLayout.razor` referenzieren diese Konstanten statt Strings zu
wiederholen. **Einschränkung:** Blazors `@page`-Direktive verlangt ein Literal, kann also nicht auf
`AppRoutes` verweisen — bei einer Routenänderung müssen `@page "..."` in der Zielseite **und** die
passende Konstante in `AppRoutes.cs` von Hand synchron gehalten werden.

`AppConfig.cs` und `AppClientRoles.cs` bündeln analog die B2C-Scope-Konstante bzw. die
App-Rollennamen.

## CSS-Konventionen

**Namens-Präfixe** (historisch gewachsen, nicht 1:1 mit der aktuellen Ordnerstruktur):

| Präfix | Ursprünglicher/aktueller Bereich |
|---|---|
| `sm-` | Seitenrahmen/Karten/Tabellen — mittlerweile über mehrere Use Cases hinweg geteilt (siehe unten), nicht nur Sondermerkmalsuche |
| `su-` | Stücklistenprüfung |
| `aa-` | Auftragsanlage |
| `la-` | Lieferantenassistent |

**Scoped vs. global:** Wo eine CSS-Klasse nur von einer Seite oder Komponente gerendert wird,
liegt ihr Stylesheet direkt daneben als `*.razor.css` (Blazors CSS-Isolation — greift automatisch
nur auf Elemente, die diese Komponente selbst rendert). Klassen, die von **mehreren** Use Cases
genutzt werden (`.sm-page`, `.sm-card`, `.sm-actions`, `.sm-merkmal-table`, `.sm-choice-grid`,
`.sm-add*`, `.aa-checkliste`, `.aa-hinweis--*`, `.su-review__meta`, `.su-review__empty`), bleiben
absichtlich in der globalen `wwwroot/app.css` — Blazor hat keinen Mechanismus, um scoped CSS
zwischen unabhängigen Komponenten zu teilen, ohne sie zu duplizieren.

**Dark-Theme-Variablen** — in `wwwroot/app.css` unter `:root` definiert, überall per `var(...)`
verwenden statt neuer hartcodierter Farben:

```
--color-bg, --color-bg-elevated, --color-bg-panel, --color-border,
--color-text, --color-text-soft, --color-text-muted,
--color-accent, --color-accent-strong,
--color-success(-bg/-border), --color-warning(-bg/-border), --color-danger(-bg/-border)
```

**Cross-Component-Styling:** Rendert eine Komponente Inhalt für eine andere (z. B. ein
`RenderFragment`-Parameter), zählt das CSS-Scoping nach der Datei, in der das Markup
**geschrieben** steht, nicht nach der Komponente, die es letztlich anzeigt. Wo eine
Elternkomponente den Zustand eines Kindes stylen müsste (z. B. "PDF-Panel eingeklappt"), löst das
Kind das lieber selbst über eine eigene Modifier-Klasse (`.su-review__pdf--eingeklappt` in
`PdfViewerPanel.razor`), statt einen `::deep`-Selektor über die Komponentengrenze zu schreiben.

## Wiederverwendbare Komponenten

- `Components/FileDropzone.razor` — Datei-Upload-Zone (Drag & Drop + Klick). Zeigt bei jedem
  Upload/jeder Analyse **immer dieselbe** Scan-Animation (Idle-/Busy-Text konfigurierbar) — genutzt
  von allen vier Upload-Stellen der App (Auftragsanlage Vertrieb/Innendienst, Stücklistenprüfung
  Schritt 1 + 3), damit "Dokument wird analysiert" überall gleich aussieht statt an jeder Stelle neu
  gebaut zu werden.
- `Components/Modal.razor` — einfacher Overlay-Dialog (Titel, `ChildContent`, `Actions`).
- `Components/ChecklistItem.razor` — ein Checklisten-Eintrag (Status-Haken, Wert, optional
  editierbarer oder nur angezeigter Kommentar) — genutzt in beiden Auftragsanlage-Seiten.
- `Components/StepIndicator.razor` — der Schritt-Stepper im Seitenkopf (`MainLayout`) bzw. inline
  in der Stücklistenprüfung.

## Tests

`Illig-AI-Platform.Tests/UI/*DesignTests.cs` prüfen per `File.ReadAllText` gezielt Inhalte von
`.razor`/`.razor.css`-Dateien (z. B. "enthält die Seite `<KeineBerechtigung />`?" oder "steht diese
CSS-Regel in der erwarteten Datei?"). Wird eine Datei verschoben/umbenannt oder Markup in eine neue
Komponente ausgelagert, müssen die entsprechenden Pfade/Assertions dort mit angepasst werden.
