namespace NatuFurniture.Domain;

public class Order
{
    private readonly List<Furniture> items = new();

    public int Id { get; }
    public Customer Customer { get; }
    public IReadOnlyList<Furniture> Items => items;
    public Payment Payment { get; private set; } = null!;
    public OrderStatus Status { get; internal set; } = OrderStatus.Placed;
    public DateTime PlacedOn { get; internal set; } = DateTime.Now;

    public Order(int id, Customer customer)
    {
        Id = id;
        Customer = customer;
    }

    public void AddItem(Furniture item)
    {
        if (item.Status != FurnitureStatus.InStock)
            throw new OutOfStockException(item.Name);
        items.Add(item);
    }

    public decimal CalculateTotal() => items.Sum(i => i.CurrentPrice);

    public void SetPayment(Payment payment)
    {
        if (payment is CreditPayment && Customer.IsBlacklisted)
            throw new StoreException("This customer is blacklisted and cannot buy on credit.");
        AttachPayment(payment);
    }

    internal void AttachPayment(Payment payment)
    {
        Payment = payment;
        payment.Order = this;
    }

    internal void RestoreItem(Furniture item) => items.Add(item);
}
