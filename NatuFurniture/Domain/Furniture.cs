namespace NatuFurniture.Domain;

public abstract class Furniture
{
    public int Id { get; private set; }
    public string Name { get; }
    public decimal BasePrice { get; internal set; }
    public FurnitureCondition Condition { get; private set; } = FurnitureCondition.New;
    public FurnitureStatus Status { get; internal set; }
    public decimal CurrentPrice { get; internal set; }
    public DateTime AddedOn { get; internal set; } = DateTime.Today;
    public abstract int WarrantyMonths { get; }
    public string TypeName => GetType().Name;

    protected Furniture(int id, string name, decimal price)
    {
        Id = id;
        Name = name;
        BasePrice = price;
        CurrentPrice = price;
        Status = FurnitureStatus.InStock;
    }

    public void Repossess(decimal resalePrice)
    {
        Condition = FurnitureCondition.SecondHand;
        Status = FurnitureStatus.Repossessed;
        CurrentPrice = resalePrice;
    }

    public abstract string Describe();

    protected string Tail => $"RM {CurrentPrice:N2}{(Condition == FurnitureCondition.SecondHand ? " [second-hand]" : "")}";

    // One physical unit sold from a catalogue entry gets its own ID so it can be repossessed individually.
    internal Furniture CloneUnit(int newId)
    {
        var copy = (Furniture)MemberwiseClone();
        copy.Id = newId;
        copy.Status = FurnitureStatus.InStock;
        return copy;
    }

    internal void RestoreState(FurnitureCondition condition, FurnitureStatus status, decimal current, DateTime addedOn)
    {
        Condition = condition;
        Status = status;
        CurrentPrice = current;
        AddedOn = addedOn;
    }
}

public class Sofa : Furniture
{
    public int Seats { get; }
    public string Material { get; }
    public override int WarrantyMonths => 24;

    public Sofa(int id, string name, decimal price, int seats, string material) : base(id, name, price)
    {
        Seats = seats;
        Material = material;
    }

    public override string Describe() => $"Sofa - {Name} ({Seats} seats, {Material}) - {Tail}";
}

public class Table : Furniture
{
    public int LengthCm { get; }
    public int WidthCm { get; }
    public override int WarrantyMonths => 12;

    public Table(int id, string name, decimal price, int lengthCm, int widthCm) : base(id, name, price)
    {
        LengthCm = lengthCm;
        WidthCm = widthCm;
    }

    public override string Describe() => $"Table - {Name} ({LengthCm}x{WidthCm} cm) - {Tail}";
}

public class Bed : Furniture
{
    public static readonly string[] Sizes = { "Single", "Double", "Queen", "King" };
    public string Size { get; }
    public override int WarrantyMonths => 36;

    public Bed(int id, string name, decimal price, string size) : base(id, name, price) => Size = size;

    public override string Describe() => $"Bed - {Name} ({Size}) - {Tail}";
}

public class Wardrobe : Furniture
{
    public int Doors { get; }
    public override int WarrantyMonths => 24;

    public Wardrobe(int id, string name, decimal price, int doors) : base(id, name, price) => Doors = doors;

    public override string Describe() => $"Wardrobe - {Name} ({Doors} doors) - {Tail}";
}
