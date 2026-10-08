using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FunBooksAndVideos.Api.ErrorHandling;

/// <summary>
/// Writes a problem document whatever the client's <c>Accept</c> header says. The framework's
/// <see cref="IProblemDetailsService"/> declines requests that do not accept JSON; this API is JSON-only,
/// so in that case the document is written directly after applying the same customisation
/// (<c>traceId</c>, default <c>code</c>) the service would have applied.
/// </summary>
internal static class ProblemResponseWriter
{
    private const string ProblemJson = "application/problem+json";

    public static async Task WriteAsync(HttpContext httpContext, ProblemDetails problem, IProblemDetailsService problemDetailsService, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(problemDetailsService);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        };

        if (await problemDetailsService.TryWriteAsync(context))
        {
            return;
        }

        problem.Title ??= ReasonPhrases.GetReasonPhrase(httpContext.Response.StatusCode);
        httpContext.RequestServices.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value.CustomizeProblemDetails?.Invoke(context);

        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), jsonOptions, ProblemJson, httpContext.RequestAborted);
    }
}
