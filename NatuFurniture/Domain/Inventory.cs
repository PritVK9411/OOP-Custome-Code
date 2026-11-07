namespace NatuFurniture.Domain;

// Observer pattern: Inventory is the subject, Staff are the observers.
public class Inventory
{
    private readonly Dictionary<int, int> stock = new();
    private readonly List<Furniture> items = new();
    private readonly List<IObserver> observers = new();

    public int LowStockThreshold { get; } = 3;
    public IReadOnlyList<Furniture> Items => items;

    public void Attach(IObserver o) { if (!observers.Contains(o)) observers.Add(o); }
    public void Detach(IObserver o) => observers.Remove(o);
    public void Notify(string message) { foreach (var o in observers.ToList()) o.Update(message); }

    public Furniture? Find(int id) => items.FirstOrDefault(i => i.Id == id);
    public int GetStock(int id) => stock.TryGetValue(id, out var q) ? q : 0;

    public void AddItem(Furniture item, int quantity)
    {
        items.Add(item);
        stock[item.Id] = quantity;
        item.Status = quantity > 0 ? FurnitureStatus.InStock : FurnitureStatus.Sold;
    }

    public void Reserve(int furnitureId)
    {
        var item = Find(furnitureId);
        if (item == null || GetStock(furnitureId) <= 0)
            throw new OutOfStockException(item?.Name ?? $"item #{furnitureId}");

        stock[furnitureId]--;
        if (stock[furnitureId] == 0) item.Status = FurnitureStatus.Sold;
        CheckLow(item);
    }

    public void Restock(Furniture item, int quantity)
    {
        if (!items.Contains(item)) items.Add(item);
        stock[item.Id] = GetStock(item.Id) + quantity;
        if (stock[item.Id] > 0) item.Status = FurnitureStatus.InStock;
    }

    public void SetStock(int furnitureId, int quantity)
    {
        var item = Find(furnitureId) ?? throw new StoreException("Furniture item not found.");
        if (quantity < 0) throw new StoreException("Stock cannot be negative.");
        stock[furnitureId] = quantity;
        item.Status = quantity > 0 ? FurnitureStatus.InStock : FurnitureStatus.Sold;
        CheckLow(item);
    }

    private void CheckLow(Furniture item)
    {
        int left = GetStock(item.Id);
        if (left < LowStockThreshold)
            Notify($"[Alert] Low stock: {item.Name} - only {left} left in stock");
    }

    public List<Furniture> Search(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return items.ToList();
        text = text.Trim();
        return items.Where(i =>
            i.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            i.TypeName.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
