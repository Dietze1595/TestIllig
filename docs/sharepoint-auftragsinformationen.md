# SharePoint-Auftragsinformationen

Die Auftragsinformationen werden aus SharePoint gelesen und mit Azure Document Intelligence
analysiert. Das Original-PDF wird **nicht** in Azure Blob Storage kopiert. Die Datenbank enthaelt
nur die SharePoint-Referenz, den ETag/Versionsstand, Verarbeitungsstatus und die extrahierten
Fachdaten.

## Quelle

- Host: `illiggroup.sharepoint.com`
- Site: `/sites/DVN_SAP_DE`
- Bibliothek: `Freigegebene Dokumente`
- technisch freigegebener Ordner: `Projekt Novazoon/99_SAP_Dokument_Transfer`
- synchronisierter Unterordner: `Auftragsinformationen`

Beim ersten Lauf werden PDF-Dateien beruecksichtigt, deren SharePoint-Aenderungsdatum innerhalb
der konfigurierten letzten fuenf Jahre liegt. Anschliessend wird der von Microsoft Graph gelieferte
`deltaLink` gespeichert. Dadurch werden nur neue, geaenderte oder geloeschte Eintraege verarbeitet.
Fehlgeschlagene Einzeldateien bleiben mit Fehlerstatus in der Datenbank und werden beim naechsten
Lauf erneut versucht; sie blockieren den Delta-Cursor nicht.

## Azure-/Microsoft-365-Einrichtung

1. Im Novazoon-Tenant eine mandantenfaehige App Registration fuer den SharePoint-Leser anlegen.
2. Microsoft Graph Application Permission `Files.SelectedOperations.Selected` hinzufuegen.
3. Ein ILLIG-Administrator erteilt im ILLIG-Tenant Admin Consent fuer die Enterprise Application.
4. Ein ILLIG-SharePoint-Administrator erteilt dieser App die Rolle `read` auf dem DriveItem des
   Ordners `99_SAP_Dokument_Transfer`. Diese Berechtigung gilt fuer dessen Unterordner, nicht fuer
   die uebrige Dokumentbibliothek oder Site.
5. `DriveId` und die Item-ID dieses freigegebenen Elternordners in die Konfiguration uebernehmen.
6. Die App Registration vertraut per Federated Credential der User Assigned Managed Identity des
   App Service. Diese tauscht ihr Novazoon-Token gegen ein App-Token des ILLIG-Tenants; ein Secret
   oder Zertifikat ist nicht erforderlich. Der B2C-Tenant ist daran nicht beteiligt.
7. Sicherstellen, dass Azure Document Intelligence konfiguriert und App Service `Always On`
   aktiviert ist.

## Konfiguration

```json
"SharePointAuftragsinformationen": {
  "Enabled": true,
  "HostName": "illiggroup.sharepoint.com",
  "SitePath": "/sites/DVN_SAP_DE",
  "LibraryName": "Freigegebene Dokumente",
  "FolderPath": "Projekt Novazoon/99_SAP_Dokument_Transfer/Auftragsinformationen",
  "DriveId": "",
  "PermissionRootItemId": "",
  "SyncSubfolderPath": "Auftragsinformationen",
  "HistoricalYears": 5,
  "PollingMinutes": 60,
  "MaxFileSizeMegabytes": 50,
  "ResourceTenantId": "",
  "ApplicationClientId": "",
  "ManagedIdentityClientId": ""
}
```

Die konkreten Werte werden pro Umgebung als App-Service-Environment-Variablen gesetzt. .NET
ersetzt dabei den Doppelpunkt eines Konfigurationspfads durch zwei Unterstriche:

```text
SharePointAuftragsinformationen__Enabled=true
SharePointAuftragsinformationen__ResourceTenantId=70c6a234-09a1-4970-bad6-9b11870e8a97
SharePointAuftragsinformationen__ApplicationClientId=dcc60cbc-d56f-444c-9678-e5f2fd41a197
SharePointAuftragsinformationen__ManagedIdentityClientId=64fe1cd6-1180-4750-81d1-4c372994ed40
SharePointAuftragsinformationen__DriveId=b!M7oaqo17_EiRwJbtGJglw21ny6rnPx1Li2Rocx8g7jo_rHd6qjWwRqJYjzL0wWi1
SharePointAuftragsinformationen__PermissionRootItemId=01ETP5AZULP2RCRKN4EVFJI7G32JXHZ6E6
```

Die drei Identitaetswerte muessen gemeinsam gesetzt sein; eine unvollstaendige
Cross-Tenant-Konfiguration wird beim Erzeugen des Graph-Credentials abgelehnt. Bei Deployment
Slots sollten mindestens `Enabled` und die SharePoint-Zielwerte als Slot Settings markiert werden.

Die ILLIG-Ordnerberechtigung wurde mit Rolle `read` fuer die Application Client ID gesetzt. Die
zurueckgegebene Permission-ID lautet
`aTowaS50fG1zLnNwLmV4dHxkY2M2MGNiYy1kNTZmLTQ0NGMtOTY3OC1lNWYyZmQ0MWExOTdANzBjNmEyMzQtMDlhMS00OTcwLWJhZDYtOWIxMTg3MGU4YTk3`.

Die Funktion ist standardmaessig deaktiviert. Sie darf erst nach Anwendung der EF-Migration und
Vergabe der Graph-Berechtigung aktiviert werden.

## API und WebApp

- `GET /api/v1/auftragsinformationen` liefert die letzten 200 erfolgreich analysierten Dokumente.
- `GET /api/v1/auftragsinformationen/{id}` liefert extrahierte Details und Merkmale.
- `GET /api/v1/auftragsinformationen/{id}/dokument` streamt das aktuelle PDF direkt aus SharePoint.
- `POST /api/v1/auftragsinformationen/synchronisieren` startet als Admin einen manuellen Lauf.

Die SharePoint-Dokumente erscheinen in der Dokumenten-Seitenleiste der Stuecklistenpruefung. Der
PDF-Inhalt wird erst beim Oeffnen eines Eintrags aus SharePoint geladen. Alle Endpunkte unterliegen
weiterhin der Rolle `PlausibilityCheck`; der manuelle Import benoetigt zusaetzlich `Admin`.

Bei App-Service-Scale-out muss der Scheduler entweder auf genau einer Instanz laufen oder spaeter
durch einen zentralen Scheduler (zum Beispiel Azure Functions Timer Trigger) ersetzt werden. Die
Eindeutigkeit von `DriveId + ItemId` verhindert doppelte Datenbankzeilen, aber nicht doppelte
Document-Intelligence-Kosten bei exakt gleichzeitigen Laeufen auf mehreren Instanzen.
