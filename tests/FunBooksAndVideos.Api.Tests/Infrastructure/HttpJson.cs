using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Tests.Infrastructure;

internal static class HttpJson
{
    public const string ProblemContentType = "application/problem+json";

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostJsonAsync<TBody>(this HttpClient client, string url, TBody body) =>
        client.PostAsJsonAsync(url, body, Options);

    /// <summary>Posts <paramref name="body"/> as JSON with an <c>Idempotency-Key</c> header.</summary>
    public static async Task<HttpResponseMessage> PostJsonWithIdempotencyKeyAsync<TBody>(this HttpClient client, string url, TBody body, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: Options),
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);
        return value ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");
    }

    public static Task<ValidationProblemDetails> ReadProblemAsync(this HttpResponseMessage response) =>
        response.ReadAsAsync<ValidationProblemDetails>();

    /// <summary>The stable <c>code</c> extension every problem response carries.</summary>
    public static string? Code(this ProblemDetails problem) =>
        problem.Extensions.TryGetValue("code", out var value) && value is JsonElement element ? element.GetString() : null;

    public static async Task<long> CreateCustomerAsync(this HttpClient client, string name = "Ada Lovelace", bool withAddress = true)
    {
        var response = await client.PostJsonAsync("/api/v1/customers", Requests.Customer(name, withAddress));
        response.EnsureSuccessStatusCode();
        return (await response.ReadAsAsync<CustomerDto>()).Id;
    }

    public static async Task<SubmitPurchaseOrderResult> SubmitOrderAsync(this HttpClient client, long customerId, params object[] lines)
    {
        var response = await client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(customerId, null, lines));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<SubmitPurchaseOrderResult>();
    }
}
