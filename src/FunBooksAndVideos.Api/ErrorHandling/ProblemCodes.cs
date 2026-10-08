using System.Globalization;

namespace FunBooksAndVideos.Api.ErrorHandling;

/// <summary>
/// Default <c>code</c> extension for problem documents the framework produces itself (unknown routes,
/// unsupported media types, ...), so that every error carries a stable machine-readable code.
/// </summary>
internal static class ProblemCodes
{
    /// <summary>Returns the default code for an HTTP status.</summary>
    public static string ForStatus(int status) =>
        status switch
        {
            StatusCodes.Status400BadRequest => RequestValidationProblemDetails.Code,
            StatusCodes.Status404NotFound => "resource.not_found",
            StatusCodes.Status405MethodNotAllowed => "method.not_allowed",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status413PayloadTooLarge => "request.too_large",
            StatusCodes.Status415UnsupportedMediaType => "media_type.unsupported",
            StatusCodes.Status422UnprocessableEntity => "unprocessable",
            StatusCodes.Status500InternalServerError => "internal_error",
            _ => string.Create(CultureInfo.InvariantCulture, $"http.{status}"),
        };
}
