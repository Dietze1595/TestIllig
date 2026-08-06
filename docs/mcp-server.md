# ILLIG MCP-Server

Der MCP-Server ist Bestandteil der bestehenden ASP.NET-Anwendung. Er stellt ausschließlich
lesende, fachlich begrenzte Werkzeuge bereit und verwendet denselben EF-Core-`AppDbContext`
wie die Webanwendung.

## Endpunkte

- MCP (Streamable HTTP): `https://<app-host>/mcp`
- OAuth Protected Resource Metadata:
  `https://<app-host>/.well-known/oauth-protected-resource/mcp`

Der MCP-Transport ist zustandslos. Dadurch ist keine Session-Affinität zwischen mehreren
App-Service-Instanzen erforderlich.

## Berechtigung

Alle MCP-Werkzeuge verlangen:

- ein gültiges Azure-AD-B2C-Bearer-Token für die vorhandene ILLIG-API;
- den Scope
  `https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access`;
- die in der ILLIG AI Platform vergebene Rolle `SearchSystem`.

Die Rolle wird weiterhin aus der ILLIG-Datenbank geladen. Es gibt keinen anonymen Zugriff und
keinen direkten Datenbankzugriff durch ChatGPT.

## Verfügbare Werkzeuge

| Werkzeug | Zweck |
| --- | --- |
| `search_customers` | Kunden nach Name, Nummer oder Adresse suchen |
| `get_customer_context` | Angebote, Kundenbestellungen und Maschinenaufträge eines Kunden abrufen |
| `search_commercial_documents` | Angebote und Kundenbestellungen anhand ihrer Referenz suchen |
| `search_supplier_commitments` | Offene Lieferantenpositionen und Lieferstatus abrufen |
| `search_bill_of_materials` | Stücklistenpositionen nach Auftrag oder Artikel suchen |

Alle Ergebnisse sind auf höchstens 50 Einträge begrenzt. Nicht ausgegeben werden insbesondere:

- Dokumentvolltexte und Dateien;
- Blob-Pfade;
- Benutzerprofile;
- Lieferanten-E-Mail-Adressen;
- Einkaufspreise;
- Datenbank-Zugangsdaten.

## Azure-Konfiguration

Die Anwendung liest folgende nicht geheime Einstellung:

```text
Mcp__AuthorizationServer=https://illigaiplatform.b2clogin.com/illigaiplatform.onmicrosoft.com/B2C_1A_IlligMultiTenantSignUpSignIn/v2.0
```

Sie ist bereits in `appsettings.json` hinterlegt und kann als App-Service-Einstellung
überschrieben werden.

Für ChatGPT muss in der passenden B2C-/Entra-App-Registrierung zusätzlich der Callback eingetragen
werden, den ChatGPT beim Anlegen der MCP-Verbindung anzeigt:

```text
https://chatgpt.com/connector/oauth/<callback_id>
```

Die Registrierung muss Authorization Code mit PKCE sowie den oben genannten API-Scope erlauben.
Azure AD B2C bietet keine dynamische Client-Registrierung. Deshalb muss in ChatGPT ein vordefinierter
OAuth-Client mit der zugehörigen Client-ID verwendet werden. Falls die bestehende B2C-Policy den vom
MCP-Standard verwendeten `resource`-Parameter nicht akzeptiert beziehungsweise nicht als Audience in
das Token übernimmt, ist vor B2C ein MCP-kompatibler OAuth-Broker erforderlich. Der MCP-Endpunkt und
die Werkzeugimplementierung bleiben davon unverändert.

## Verbindung in ChatGPT testen

1. Anwendung in den Dev-Slot deployen.
2. Metadaten-URL im Browser oder mit `curl` aufrufen und `authorization_servers` sowie
   `scopes_supported` prüfen.
3. Dem Testbenutzer in der ILLIG AI Platform die Rolle `SearchSystem` geben.
4. In ChatGPT den Entwicklermodus aktivieren.
5. Eine neue Plugin-/MCP-Verbindung mit `https://<dev-host>/mcp` anlegen.
6. Den von ChatGPT angezeigten OAuth-Callback in B2C/Entra hinterlegen und die Verbindung anmelden.
7. Die erkannten fünf Werkzeuge kontrollieren.
8. Zunächst direkte Werkzeugfragen testen, danach SharePoint und den ILLIG-MCP-Server gemeinsam
   in einer neuen Unterhaltung aktivieren.

Beispiel:

> Welche Abweichungen zwischen Angeboten und Kundenbestellungen treten bei Kunde X auf, und welche
> Arbeitsanweisungen im SharePoint behandeln diese Fälle?

## Lokale Prüfung

```powershell
dotnet test Illig-AI-Platform.Tests\Illig-AI-Platform.Tests.csproj `
  --filter "FullyQualifiedName~IlligKnowledgeToolsTests|FullyQualifiedName~McpEndpoint|FullyQualifiedName~McpResourceMetadata"
```

Jeder Werkzeugaufruf wird mit Werkzeugname, Benutzer-Object-ID und Anzahl der Ergebnisse über das
vorhandene ASP.NET-Logging protokolliert. Suchbegriffe und Ergebnisinhalte werden nicht in das
Audit-Log geschrieben.
