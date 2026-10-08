using Microsoft.OpenApi;

namespace FunBooksAndVideos.Api.Swagger;

internal static class SwaggerConfiguration
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "FunBooksAndVideos API",
                Version = DocumentName,
                Description =
                    "E-commerce back end for books, online videos and club memberships.\n\n" +
                    "Submitting a purchase order runs the purchase order processor, which applies the business rules: " +
                    "BR1 - memberships are activated on the customer account immediately; " +
                    "BR2 - a shipping slip is generated for physical products.\n\n" +
                    "Demo data: customer 4567890 (Jane Doe), products 1-4 and the three membership plans are pre-loaded; " +
                    "the first submitted order receives id 3344656.",
            });

            foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "FunBooksAndVideos.*.xml"))
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<OrderLineRequestSchemaFilter>();
        });

        return services;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{DocumentName}/swagger.json", $"FunBooksAndVideos API {DocumentName}");
            options.DocumentTitle = "FunBooksAndVideos API";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
