namespace Illig_AI_Platform.Client;

/// <summary>
/// App-weite Konstanten, die an mehreren Stellen unabhängig voneinander gebraucht werden
/// (z. B. beim initialen Login in <c>Program.cs</c> und beim Nachfordern eines Tokens in
/// <see cref="Layout.MainLayout"/>) — eine Quelle, damit eine Änderung nicht an einer Stelle
/// vergessen wird.
/// </summary>
public static class AppConfig
{
    public const string ApiScope =
        "https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access";
}
