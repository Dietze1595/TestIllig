namespace Illig_AI_Platform.Mcp;

public static class McpConfiguration
{
    public const string AuthenticationScheme = "IlligMcp";
    public const string ReadScope =
        "https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access";

    public const string SecuritySchemesJson =
        """[{"type":"oauth2","scopes":["https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access"]}]""";
}
