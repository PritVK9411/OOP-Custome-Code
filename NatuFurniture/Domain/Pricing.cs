namespace NatuFurniture.Domain;

public class FlatDiscountStrategy : IPricingStrategy
{
    private readonly decimal percent;
    public FlatDiscountStrategy(decimal percent = 50m) => this.percent = percent;

    public decimal CalculatePrice(Furniture item) =>
        Math.Round(item.BasePrice * (1 - percent / 100m), 2);
}

public class AgeDepreciationStrategy : IPricingStrategy
{
    public AgeDepreciationStrategy() { }

    public decimal CalculatePrice(Furniture item)
    {
        var today = DateTime.Today;
        int months = (today.Year - item.AddedOn.Year) * 12 + today.Month - item.AddedOn.Month;
        if (months < 0) months = 0;
        decimal factor = Math.Max(0.30m, 1m - 0.02m * months);
        return Math.Round(item.BasePrice * factor, 2);
    }
}
