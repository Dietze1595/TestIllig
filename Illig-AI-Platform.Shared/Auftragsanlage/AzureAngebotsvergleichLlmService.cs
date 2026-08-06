using System.Text.Json;
using OpenAI.Chat;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Vergleicht Angebot und Kundenbestellung per Azure-OpenAI-Chatmodell anhand des vollen
/// erkannten Dokumenttexts (statt einzelner Felder) — robust gegenüber Layout-Variationen,
/// vertauschten Kunde/Vendor-Rollen, unterschriebenen Angebotskopien und unterschiedlicher
/// Positions-Granularität zwischen Angebot und Kundenbestellung.
/// </summary>
public class AzureAngebotsvergleichLlmService(ChatClient chatClient) : IAngebotsvergleichLlmService
{
    private const string SystemAnweisung = """
        Du vergleichst ein Angebot mit der dazugehörigen Kundenbestellung (Purchase Order).
        Das Ergebnis ist eine Entscheidungshilfe für den Innendienst. Arbeite ausschließlich
        mit den beiden Dokumenttexten, erfinde keine Werte und antworte auf Deutsch.

        Die Dokumente können aus beliebigen ERP-Systemen stammen und völlig unterschiedliche
        Layouts, Positionsnummern, Tabellenstrukturen, Sprachen und Bezeichnungen verwenden.
        Ordne Inhalte deshalb semantisch zu und nicht anhand ihrer Position im Dokument.

        Prüfe zuerst die Art des zweiten Dokuments:
        - "istBestelldokument" ist nur dann true, wenn das zweite Dokument eine Kundenbestellung,
          Purchase Order, verbindliche Auftragserteilung oder eine eindeutig unterschriebene
          Annahme des Angebots ist.
        - Ein reines Angebot, eine unverbindliche Angebotskopie oder ein sonstiges Dokument ohne
          erkennbare Bestellung bzw. Annahme ist kein Bestelldokument.
        - Verlasse dich bei dieser Entscheidung auf den Inhalt des Dokuments und nicht auf die
          Bezeichnung "KUNDENBESTELLUNG" in der Nutzernachricht.
        - Erkläre die Entscheidung kurz und konkret in "dokumentartHinweis".
        - Ist das zweite Dokument kein Bestelldokument, lasse "pruefbereiche" leer und
          führe keinen fachlichen Vergleich durch.

        Prüfe diese Bereiche:
        1. Liefertermin: Vergleiche Lieferdatum oder Lieferdauer des Angebots mit dem von der
           Bestellung gewünschten oder bestätigten Termin. Gib beide Werte separat zurück.
        2. Angebotsbezug: Stimmen Angebotsnummer sowie genanntes Angebotsdatum bzw. die
           erkennbare Version überein? Die Bestellnummer ist eine eigene Nummer und deshalb
           keine Abweichung.
        3. Kunde und Lieferanschrift (Bereich "kunde_lieferanschrift"): Vergleiche bestellende
           Gesellschaft, Warenempfänger und die vollständige Lieferadresse einschließlich
           Straße. Verwechsle ILLIG als Lieferant nicht mit dem Kunden. Adressbefunde gehören
           ausschließlich in diesen Bereich. Eine beim Lieferanten angegebene Anschrift oder
           ein Feld "Address" direkt unter dem Lieferantennamen ist keine Lieferanschrift.
           Ist "Del Place", "Ship To" oder ein vergleichbares Empfängerfeld leer, melde die
           Lieferanschrift als fehlend und erfinde keinen Empfänger aus Lieferantendaten.
        4. Leistungsumfang: Vergleiche kaufmännische Hauptpositionen, Material-/Maschinentypen,
           Optionen und Mengen semantisch. Ignoriere unterschiedliche Positionsnummern,
           Formulierungen und die feinere Aufteilung von Paket- oder Unterpositionen. Melde
           fehlende, zusätzliche oder inhaltlich geänderte Leistungen. Bestellt die Bestellung
           erkennbar nur eine konkrete Angebotsposition und passen Beschreibung, Menge und Preis
           genau zu dieser Position, behandle andere Angebotspositionen nicht automatisch als
           fehlend. Melde sie nur dann als Abweichung, wenn die Bestellung ausdrücklich das
           Gesamtangebot annimmt oder die Leistungen laut Angebot untrennbar zusammengehören.
        5. Preis und Währung: Vergleiche Nettogesamtpreis, positionsbezogene Preise soweit
           eindeutig zuordenbar, Rabatte und Währung. Beachte deutsche und englische
           Zahlenformate. Melde auch kleine echte Differenzen, aber keine reinen Rundungs-
           oder Darstellungsunterschiede ohne Wertänderung.
        6. Zahlungsbedingungen: Vergleiche Zahlungsziel und Zahlungsplan einschließlich
           Prozentsätzen, Festbeträgen, Meilensteinen, Bürgschaften und Fälligkeiten.
        7. Lieferbedingungen (Bereich "lieferbedingungen"): Vergleiche Incoterm, benannten
           Incoterm-Ort, Versandart, Fracht, Verpackung und Montage/Inbetriebnahme, soweit in
           beiden Dokumenten geregelt. Kunden- und Lieferadressen gehören nicht hierher,
           sondern ausschließlich in "kunde_lieferanschrift".
        8. Abnahme und Gewährleistung: Vergleiche FAT/SAT, Abnahmekriterien,
           Gewährleistungsdauer und relevante Fristen.
        9. Vertragsrisiken: Melde neu eingeführte oder widersprechende Einkaufsbedingungen,
           Haftung, Vertragsstrafen, Stornierung, anwendbares Recht, Gerichtsstand,
           Dokumentationspflichten und sonstige kaufmännisch erhebliche Klauseln.

        Regeln für die Bewertung:
        - Eine Bedingung, die in der Bestellung nicht wiederholt wird, ist nicht automatisch
          eine Abweichung, wenn die Bestellung eindeutig auf das Angebot verweist oder eine
          unverändert unterschriebene Angebotskopie ist.
        - Bei einer unterschriebenen Angebotskopie prüfst du vor allem handschriftliche
          Änderungen, Streichungen, Ergänzungen und abweichende Anlagen.
        - Gib jeden Befund als eigenes, möglichst atomares Objekt in "pruefbereiche" aus.
          "bereich" muss einen Wert aus dem Schema verwenden, "status" ist entweder
          "uebereinstimmung" oder "abweichung". "pruefschluessel" bezeichnet den konkret
          geprüften Sachverhalt in stabilem snake_case, zum Beispiel "angebotsnummer",
          "position_20_beschreibung", "position_20_menge", "gesamtpreis", "waehrung",
          "zahlungsziel", "proforma_zahlung", "incoterm" oder "lieferanschrift".
        - Prüfe und berichte separat, soweit in beiden Dokumenten vorhanden: Angebotsnummer,
          Angebotsdatum/Version, jede eindeutig zuordenbare bestellte Position, deren Menge,
          deren Preis, Nettogesamtpreis, Währung, Rabatt, Zahlungsziel, einzelne Raten oder
          Zahlungsmeilensteine, Incoterm, Incoterm-Ort und Versandart. Lasse bestätigte
          Übereinstimmungen nicht weg, nur weil ein anderer Sachverhalt desselben Bereichs
          abweicht.
        - Trenne bestätigte Übereinstimmungen strikt von echten Abweichungen:
          * Status "uebereinstimmung" gilt ausschließlich für geprüfte Bereiche, deren
            relevante Inhalte in beiden Dokumenten identisch oder semantisch gleichwertig sind.
          * Status "abweichung" gilt ausschließlich für belegbare Unterschiede oder einen
            bei einem kritischen Punkt tatsächlich nicht auflösbaren Widerspruch.
        - Gib einen geprüften Bereich niemals nur deshalb als Abweichung aus, weil du ihn
          untersucht hast. Formulierungen wie "identisch", "stimmt überein", "inhaltlich
          gleich" oder "keine Abweichung erkennbar" erhalten Status "uebereinstimmung".
        - Derselbe "pruefschluessel" darf niemals zugleich als Übereinstimmung und Abweichung
          ausgegeben werden. Ein Bereich darf jedoch mehrere unabhängige Prüfthemen mit
          unterschiedlichen Status enthalten. Beispiel: "proforma_zahlung" kann übereinstimmen,
          während "zahlungsziel" abweicht.
        - Beispiel: Sind Incoterm und Versandart identisch, ist
          "Lieferbedingungen: Incoterm und Versandart sind identisch" eine Übereinstimmung.
          Enthält die Bestellung aber zusätzlich eine relevante Klausel zu
          Bankdatenänderungen, ist "Vertragsrisiken: zusätzlicher Hinweis ..." eine Abweichung.
        - Keine allgemeinen Empfehlungen.
        - Der Text jedes Prüfbereichs ist für sich verständlich. Bei einer Abweichung nennt er
          die Werte beider Dokumente.
        - Der Liefertermin wird über die drei eigenen Liefertermin-Felder ausgegeben und nicht
          zusätzlich in "pruefbereiche" wiederholt.
        - Fasse nur denselben konkreten Sachverhalt zusammen und liefere höchstens 24 Befunde.

        Antworte ausschließlich im vorgegebenen JSON-Schema.
        """;

    private static readonly ChatCompletionOptions Options = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "vergleichsergebnis",
            jsonSchema: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "istBestelldokument": { "type": "boolean" },
                    "dokumentartHinweis": { "type": ["string", "null"] },
                    "lieferterminAngebot": { "type": ["string", "null"] },
                    "lieferterminBestaetigung": { "type": ["string", "null"] },
                    "lieferterminIdentisch": { "type": "boolean" },
                    "pruefbereiche": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "bereich": {
                            "type": "string",
                            "enum": ["angebotsbezug", "kunde_lieferanschrift", "leistungsumfang", "preis_waehrung", "zahlungsbedingungen", "lieferbedingungen", "abnahme_gewaehrleistung", "vertragsrisiken"]
                          },
                          "status": {
                            "type": "string",
                            "enum": ["uebereinstimmung", "abweichung"]
                          },
                          "pruefschluessel": { "type": "string" },
                          "text": { "type": "string" }
                        },
                        "required": ["bereich", "status", "pruefschluessel", "text"],
                        "additionalProperties": false
                      }
                    }
                  },
                  "required": ["istBestelldokument", "dokumentartHinweis", "lieferterminAngebot", "lieferterminBestaetigung", "lieferterminIdentisch", "pruefbereiche"],
                  "additionalProperties": false
                }
                """),
            jsonSchemaIsStrict: true)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
        string angebotVolltext, string bestaetigungVolltext, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemAnweisung),
            new UserChatMessage(
                $"ANGEBOT:\n{angebotVolltext}\n\nKUNDENBESTELLUNG:\n{bestaetigungVolltext}"),
        ];

        var completion = await chatClient.CompleteChatAsync(messages, Options, cancellationToken);
        var json = completion.Value.Content[0].Text;

        var antwort = JsonSerializer.Deserialize<LlmAntwort>(json, JsonOptions)
            ?? throw new InvalidOperationException("Azure OpenAI hat keine auswertbare Antwort geliefert.");

        var bereinigt = AngebotsvergleichPruefpunktBereinigung.Bereinigen(antwort.Pruefbereiche);

        return new AngebotsVergleichLlmErgebnis(
            antwort.LieferterminAngebot, antwort.LieferterminBestaetigung,
            antwort.LieferterminIdentisch, bereinigt.Uebereinstimmungen, bereinigt.Abweichungen,
            antwort.IstBestelldokument, antwort.DokumentartHinweis);
    }

    // Zwischenform fürs JSON-Deserialisieren — Property-Namen passend zum Schema oben
    // (System.Text.Json mit JsonSerializerDefaults.Web matcht camelCase automatisch).
    private record LlmAntwort(
        bool IstBestelldokument, string? DokumentartHinweis,
        string? LieferterminAngebot, string? LieferterminBestaetigung,
        bool LieferterminIdentisch, IReadOnlyList<AngebotsvergleichPruefpunkt> Pruefbereiche);
}
