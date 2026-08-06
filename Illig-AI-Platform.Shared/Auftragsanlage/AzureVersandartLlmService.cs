using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Semantischer Fallback für Layouts, bei denen Beschriftung und Wert im linearen
/// Document-Intelligence-Text nicht sicher einander zugeordnet werden können.
/// </summary>
public class AzureVersandartLlmService(ChatClient chatClient) : IVersandartLlmService
{
    private const string SystemAnweisung = """
        Du extrahierst ausschließlich die Versandart aus einem Angebot.

        Gemeint ist die konkrete Transportart, die beispielsweise mit "type of transport",
        "mode of transport", "shipping method" oder "Versandart" beschriftet sein kann.
        Beispiele sind "pick-up", "by seafreight", "air freight", "forwarder" oder
        "Economy (DPI)". Incoterms, Lieferorte, Versandadressen, Entladestellen und allgemeine
        Rechtstexte sind keine Versandart.

        Regeln:
        - Verwende ausschließlich den Dokumenttext und erfinde nichts.
        - Gib die Versandart exakt in der Schreibweise des Dokuments zurück; nicht übersetzen
          und nicht auf eine Kategorie vereinheitlichen.
        - "fundstelle" muss ein kurzer, zusammenhängender und exakt im Dokument vorkommender
          Textausschnitt sein, der Beschriftung und Wert gemeinsam belegt.
        - Wenn keine Versandart eindeutig belegt ist, setze "gefunden" auf false und beide
          Textfelder auf null.
        - Antworte ausschließlich im vorgegebenen JSON-Schema.
        """;

    private static readonly ChatCompletionOptions Options = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "versandartergebnis",
            jsonSchema: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "gefunden": { "type": "boolean" },
                    "versandart": { "type": ["string", "null"] },
                    "fundstelle": { "type": ["string", "null"] }
                  },
                  "required": ["gefunden", "versandart", "fundstelle"],
                  "additionalProperties": false
                }
                """),
            jsonSchemaIsStrict: true)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> ErmittleAsync(
        string angebotVolltext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(angebotVolltext))
            return null;

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemAnweisung),
            new UserChatMessage($"ANGEBOT:\n{angebotVolltext}"),
        ];

        var completion = await chatClient.CompleteChatAsync(messages, Options, cancellationToken);
        var json = completion.Value.Content[0].Text;
        var antwort = JsonSerializer.Deserialize<LlmAntwort>(json, JsonOptions);

        if (antwort is null || !antwort.Gefunden ||
            !VersandartFundstellenpruefung.IstBelegt(
                angebotVolltext, antwort.Versandart, antwort.Fundstelle))
            return null;

        return antwort.Versandart!.Trim();
    }

    private record LlmAntwort(bool Gefunden, string? Versandart, string? Fundstelle);
}

public static class VersandartFundstellenpruefung
{
    public static bool IstBelegt(string dokumenttext, string? versandart, string? fundstelle)
    {
        if (string.IsNullOrWhiteSpace(dokumenttext) || string.IsNullOrWhiteSpace(versandart) ||
            string.IsNullOrWhiteSpace(fundstelle) || versandart.Trim().Length > 120)
            return false;

        var text = Normalisieren(dokumenttext);
        var wert = Normalisieren(versandart);
        var beleg = Normalisieren(fundstelle);

        return beleg.Length <= 500 && text.Contains(beleg, StringComparison.Ordinal) &&
               beleg.Contains(wert, StringComparison.Ordinal) &&
               Regex.IsMatch(beleg,
                   @"\b(?:TYPE\s+OF\s+TRANSPORT|MODE\s+OF\s+TRANSPORT|SHIPPING\s+METHOD|VERSANDART)\b",
                   RegexOptions.CultureInvariant);
    }

    private static string Normalisieren(string wert) =>
        Regex.Replace(wert, @"\s+", " ").Trim().ToUpperInvariant();
}
