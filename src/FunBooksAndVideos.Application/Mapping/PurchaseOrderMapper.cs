using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Mapping;

public static class PurchaseOrderMapper
{
    public static PurchaseOrderDto ToDto(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new PurchaseOrderDto(
            order.Id.Value,
            order.CustomerId.Value,
            order.Total.Amount,
            order.Status,
            order.Lines.Select(line => line.Accept(OrderLineDtoVisitor.Instance)).ToList(),
            order.CreatedAt,
            order.ProcessedAt);
    }

    public static OrderLineDto ToDto(OrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return line.Accept(OrderLineDtoVisitor.Instance);
    }

    /// <summary>Maps each line type without switching on runtime types (GoF Visitor).</summary>
    private sealed class OrderLineDtoVisitor : IOrderLineVisitor<OrderLineDto>
    {
        public static readonly OrderLineDtoVisitor Instance = new();

        public OrderLineDto VisitProduct(ProductLine line) =>
            new(
                OrderLineType.Product,
                line.Description,
                line.Price.Amount,
                ProductId: line.ProductId.Value,
                ProductKind: line.ProductKind,
                RequiresShipping: line.RequiresShipping,
                MembershipType: null);

        public OrderLineDto VisitMembership(MembershipLine line) =>
            new(
                OrderLineType.Membership,
                line.Description,
                line.Price.Amount,
                ProductId: null,
                ProductKind: null,
                RequiresShipping: null,
                MembershipType: line.MembershipType);
    }
}
