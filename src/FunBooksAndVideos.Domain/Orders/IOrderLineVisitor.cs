namespace FunBooksAndVideos.Domain.Orders;

/// <summary>
/// GoF Visitor over the closed set of order line types. Code that has to treat each line type
/// differently (mapping, reporting, pricing) implements this interface instead of switching on types;
/// adding a line type then fails compilation everywhere it is not handled.
/// </summary>
public interface IOrderLineVisitor<out TResult>
{
    TResult VisitProduct(ProductLine line);

    TResult VisitMembership(MembershipLine line);
}
