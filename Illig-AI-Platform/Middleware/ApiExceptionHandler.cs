using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Middleware;

/// <summary>
/// Auffangnetz für Ausnahmen, die eine Action nicht selbst behandelt (z. B. weil sie wie
/// <see cref="Illig_AI_Platform.Controllers.Plausibilitaetspruefung.Sondermerkmalsuche.SondermerkmalController"/>
/// keinen eigenen try/catch hat). Loggt konsistent und liefert eine einheitliche 500-Antwort,
/// statt dass die Ausnahme unbehandelt bis zur SPA-Fallback-Seite durchfällt.
/// </summary>
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unbehandelte Ausnahme bei {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ein unerwarteter Fehler ist aufgetreten.",
            Detail = "Bitte versuche es erneut. Wenn das Problem bestehen bleibt, wende dich an den Support."
        }, cancellationToken);

        return true;
    }
}
