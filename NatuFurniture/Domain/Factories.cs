namespace NatuFurniture.Domain;

// Factory Method pattern
public static class FurnitureFactory
{
    public static Furniture Create(string type, int id, string name, decimal price,
        int a = 0, int b = 0, string text = "")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new StoreException("Furniture needs a name.");
        if (price <= 0) throw new StoreException("Price must be greater than zero.");

        switch ((type ?? "").Trim().ToLowerInvariant())
        {
            case "sofa":
                return new Sofa(id, name, price, a <= 0 ? 3 : a, string.IsNullOrWhiteSpace(text) ? "fabric" : text);
            case "table":
                return new Table(id, name, price, a <= 0 ? 150 : a, b <= 0 ? 90 : b);
            case "bed":
                var size = Bed.Sizes.FirstOrDefault(s => s.Equals(text, StringComparison.OrdinalIgnoreCase)) ?? "Queen";
                return new Bed(id, name, price, size);
            case "wardrobe":
                return new Wardrobe(id, name, price, a <= 0 ? 2 : a);
            default:
                throw new StoreException($"Unknown furniture type '{type}'.");
        }
    }
}

public static class UserFactory
{
    public static User Create(string role, int id, string name, string email,
        string password = "", string address = "", string phone = "")
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            throw new StoreException("Name and email are required.");

        switch ((role ?? "").Trim().ToLowerInvariant())
        {
            case "manager": return new Staff(id, name, email.Trim(), password, StaffRole.Manager);
            case "sales": return new Staff(id, name, email.Trim(), password, StaffRole.Sales);
            case "warehouse": return new Staff(id, name, email.Trim(), password, StaffRole.Warehouse);
            case "customer": return new Customer(id, name, email.Trim(), address, phone);
            default: throw new StoreException($"Unknown role '{role}'.");
        }
    }
}
