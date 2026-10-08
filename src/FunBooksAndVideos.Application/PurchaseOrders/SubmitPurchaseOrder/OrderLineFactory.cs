using System.Globalization;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>
/// Bulk-loads the referenced products and membership plans, resolves the lines in request order and reports
/// all unresolved references together, keyed by their position in the request (<c>lines[2].productId</c>).
/// Prices are taken from the catalog, never from the client.
/// </summary>
public sealed class OrderLineFactory : IOrderLineFactory
{
    private readonly IProductRepository _products;
    private readonly IMembershipPlanRepository _membershipPlans;

    public OrderLineFactory(IProductRepository products, IMembershipPlanRepository membershipPlans)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(membershipPlans);

        _products = products;
        _membershipPlans = membershipPlans;
    }

    public async Task<IReadOnlyList<OrderLine>> CreateAsync(IReadOnlyList<OrderLineRequest> requests, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            throw ValidationException.For("lines", "At least one item line is required.");
        }

        if (requests.Any(request => request is null))
        {
            throw ValidationException.For("lines", "Item lines must not contain null entries.");
        }

        var products = await LoadProductsAsync(requests, cancellationToken);
        var plans = await LoadMembershipPlansAsync(requests, cancellationToken);

        var errors = new ValidationErrors();
        var lines = new List<OrderLine>(requests.Count);

        for (var index = 0; index < requests.Count; index++)
        {
            switch (requests[index])
            {
                case ProductLineRequest productRequest:
                    if (products.TryGetValue(new ProductId(productRequest.ProductId), out var product))
                    {
                        lines.Add(ProductLine.For(product));
                    }
                    else
                    {
                        errors.Add(Member(index, "productId"), $"Product {productRequest.ProductId} does not exist.");
                    }

                    break;

                case MembershipLineRequest membershipRequest:
                    if (plans.TryGetValue(membershipRequest.MembershipType, out var plan))
                    {
                        lines.Add(MembershipLine.For(plan));
                    }
                    else
                    {
                        errors.Add(Member(index, "membershipType"), $"Membership {membershipRequest.MembershipType} is not offered.");
                    }

                    break;

                default:
                    throw new NotSupportedException($"Order line request of type {requests[index].GetType().Name} is not supported.");
            }
        }

        errors.ThrowIfAny();
        return lines;
    }

    private static string Member(int index, string property) =>
        string.Create(CultureInfo.InvariantCulture, $"lines[{index}].{property}");

    private async Task<IReadOnlyDictionary<ProductId, Product>> LoadProductsAsync(IReadOnlyList<OrderLineRequest> requests, CancellationToken cancellationToken)
    {
        var ids = requests
            .OfType<ProductLineRequest>()
            .Select(request => new ProductId(request.ProductId))
            .Distinct()
            .ToList();

        return ids.Count == 0
            ? new Dictionary<ProductId, Product>()
            : await _products.FindManyAsync(ids, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<MembershipType, MembershipPlan>> LoadMembershipPlansAsync(IReadOnlyList<OrderLineRequest> requests, CancellationToken cancellationToken)
    {
        var types = requests
            .OfType<MembershipLineRequest>()
            .Select(request => request.MembershipType)
            .Distinct();

        var plans = new Dictionary<MembershipType, MembershipPlan>();
        foreach (var type in types)
        {
            var plan = await _membershipPlans.FindAsync(type, cancellationToken);
            if (plan is not null)
            {
                plans[type] = plan;
            }
        }

        return plans;
    }
}
