-- Manuelles Cleanup, falls die Migration '20260811141338_ConsolidateAuftragsdokumente'
-- fehlgeschlagen ist, nachdem MySQL bereits einzelne ALTER TABLE ADD COLUMN-Anweisungen
-- ausgeführt hat (MySQL-DDL läuft NICHT transaktional, daher bleiben Teiländerungen
-- auch nach einem Fehler bestehen, ohne dass EF Core die Migration als angewendet vermerkt).
--
-- Dieses Skript setzt die Tabelle StuecklistenpruefungVerlaufEintraege zurück auf den
-- Stand VOR dem ersten (fehlgeschlagenen) Migrationsversuch. Danach kann
-- 'dotnet ef database update' erneut sauber ausgeführt werden.
--
-- WICHTIG: Nur ausführen, wenn die Migration noch NICHT in __EFMigrationsHistory steht!
-- Prüfen mit:
--   SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260811141338_ConsolidateAuftragsdokumente';
-- Falls dort bereits ein Eintrag existiert, NICHT dieses Skript ausführen.
--
-- Hinweis: Ohne "IF EXISTS", da diese Syntax erst ab MySQL 8.0.29 unterstützt wird.
-- Falls eine Spalte bereits nicht (mehr) existiert, bricht der jeweilige Befehl mit
-- "Error 1091 (Can't DROP ...; check that column/key exists)" ab - dann einfach diese
-- eine Zeile überspringen und mit den übrigen fortfahren.

ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN Quelle;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN SharePointDriveId;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN SharePointItemId;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN ETag;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN WebUrl;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN SharePointErstelltAm;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN SharePointGeaendertAm;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN AnalyseStatus;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN AnalyseFehler;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN VerarbeitetAm;
ALTER TABLE StuecklistenpruefungVerlaufEintraege DROP COLUMN GeloeschtAm;

-- Hinweis: UserProfileId wurde beim ersten Versuch noch NICHT verändert (der Fehler trat
-- beim zweiten Lauf schon an der allerersten AddColumn-Anweisung auf), daher hier keine
-- Rücknahme nötig. Falls dein erster Fehlschlag weiter fortgeschritten war (z. B. bis zum
-- INSERT-Statement), prüfe zusätzlich mit:
--   SHOW COLUMNS FROM StuecklistenpruefungVerlaufEintraege LIKE 'UserProfileId';
-- Ist die Spalte dort bereits "YES" (nullable), ist das kein Problem - die Migration
-- würde sie ohnehin erneut (idempotent) auf nullable setzen.
