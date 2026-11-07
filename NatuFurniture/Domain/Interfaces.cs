namespace NatuFurniture.Domain;

// Observer pattern
public interface IObserver
{
    void Update(string message);
}

// Strategy pattern
public interface IPricingStrategy
{
    decimal CalculatePrice(Furniture item);
}

// State pattern
public interface IPaymentState
{
    string Name { get; }
    void HandleOverdue(CreditPayment payment, DateTime today);
}
