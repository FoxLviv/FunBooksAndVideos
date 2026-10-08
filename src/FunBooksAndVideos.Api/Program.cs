using System.Text.Json.Serialization;
using FunBooksAndVideos.Api.ErrorHandling;
using FunBooksAndVideos.Api.Json;
using FunBooksAndVideos.Api.Swagger;
using FunBooksAndVideos.Application.DependencyInjection;
using FunBooksAndVideos.Infrastructure.DependencyInjection;
using FunBooksAndVideos.Infrastructure.Seeding;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Orders are small documents; a tight body limit bounds the parsing work a single request can cause.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 256 * 1024);

builder.Services
    .AddControllers(options =>
    {
        // Contracts declare [Required] explicitly; the implicit rule only adds noise when the body is unparseable.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // Enums as names: ordinary enums accept exactly one declared name, flags enums use the built-in converter.
        options.JsonSerializerOptions.Converters.Add(new StrictStringEnumConverterFactory());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
        context.ProblemDetails.Extensions.TryAdd(
            ApiExceptionHandler.CodeExtension,
            ProblemCodes.ForStatus(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode));
    });
builder.Services.AddRequestValidationProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(context => ProblemResponseWriter.WriteAsync(
    context.HttpContext,
    new ProblemDetails { Status = context.HttpContext.Response.StatusCode, Instance = context.HttpContext.Request.Path },
    context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()));

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseApiDocumentation();
app.MapControllers();
app.MapHealthChecks("/health").ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (app.Configuration.GetValue("DemoData:Seed", defaultValue: true))
{
    app.Services.GetRequiredService<IDemoDataSeeder>().Seed();
}

app.Run();

/// <summary>Entry point marker used by the integration tests' <c>WebApplicationFactory</c>.</summary>
public partial class Program;
