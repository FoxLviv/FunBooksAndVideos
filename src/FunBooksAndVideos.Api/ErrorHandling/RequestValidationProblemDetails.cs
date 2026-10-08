using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace FunBooksAndVideos.Api.ErrorHandling;

internal static class RequestValidationProblemDetails
{
    public const string Code = "request.invalid";

    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Makes automatic model-state failures (400) carry the same <c>code</c> extension and the same
    /// <c>application/problem+json</c> content type as every other error response, with the per-field error keys
    /// expressed in the JSON contract's camelCase (see <see cref="ModelStateKeys"/>).
    /// </summary>
    public static IServiceCollection AddRequestValidationProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                var problem = factory.CreateValidationProblemDetails(
                    context.HttpContext,
                    context.ModelState,
                    StatusCodes.Status400BadRequest,
                    title: "The request is malformed.",
                    instance: context.HttpContext.Request.Path);

                var jsonKeyedErrors = ModelStateKeys.ToJsonKeys(problem.Errors);
                problem.Errors.Clear();
                foreach (var (key, messages) in jsonKeyedErrors)
                {
                    problem.Errors[key] = messages;
                }

                problem.Extensions[ApiExceptionHandler.CodeExtension] = Code;

                return new ValidationProblemResult(problem);
            });

        return services;
    }

    /// <summary>
    /// Writes the validation problem document itself: <see cref="IProblemDetailsService"/> inside an API
    /// controller endpoint lets MVC rebuild the document as a plain <see cref="ProblemDetails"/> (dropping the
    /// per-field errors), and a direct write keeps the <c>application/problem+json</c> media type independent
    /// of content negotiation.
    /// </summary>
    private sealed class ValidationProblemResult : IActionResult
    {
        private readonly ValidationProblemDetails _problem;

        public ValidationProblemResult(ValidationProblemDetails problem)
        {
            ArgumentNullException.ThrowIfNull(problem);
            _problem = problem;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var httpContext = context.HttpContext;
            var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

            httpContext.Response.StatusCode = _problem.Status ?? StatusCodes.Status400BadRequest;

            return httpContext.Response.WriteAsJsonAsync(
                _problem,
                _problem.GetType(),
                jsonOptions,
                contentType: $"{ProblemJson}; charset={System.Text.Encoding.UTF8.WebName}",
                httpContext.RequestAborted);
        }
    }
}
