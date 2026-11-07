using System.Security.Cryptography;
using System.Text;

namespace NatuFurniture.Domain;

public abstract class User
{
    public int Id { get; }
    public string Name { get; }
    public string Email { get; }

    protected User(int id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }

    public abstract bool Login(string credential);
    public abstract string Describe();
}

public class Staff : User, IObserver
{
    private readonly string passwordHash;
    private readonly List<string> notifications = new();

    public StaffRole Role { get; }
    public int FailedLogins { get; private set; }
    public IReadOnlyList<string> Notifications => notifications;
    public string PasswordHash => passwordHash;

    public Staff(int id, string name, string email, string passwordOrHash, StaffRole role, bool alreadyHashed = false)
        : base(id, name, email)
    {
        passwordHash = alreadyHashed ? passwordOrHash : Hash(passwordOrHash);
        Role = role;
    }

    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));
        return Convert.ToHexString(bytes);
    }

    public override bool Login(string password)
    {
        if (FailedLogins >= 3)
            throw new AuthenticationException("This account is locked after 3 failed attempts. Ask a manager to unlock it.");

        if (Hash(password) == passwordHash)
        {
            FailedLogins = 0;
            return true;
        }

        FailedLogins++;
        throw new AuthenticationException(FailedLogins >= 3
            ? "Wrong password. The account is now locked."
            : $"Wrong password. {3 - FailedLogins} attempt(s) left.");
    }

    internal void Unlock() => FailedLogins = 0;
    internal void RestoreFailedLogins(int n) => FailedLogins = n;

    public bool HasPermission(string action)
    {
        if (Role == StaffRole.Manager) return true; // full access
        return Role switch
        {
            StaffRole.Sales => action is "RegisterCustomer" or "PlaceOrder" or "TakePayment" or "ViewOrders",
            StaffRole.Warehouse => action is "UpdateStock",
            _ => false
        };
    }

    public void Update(string message)
    {
        notifications.Insert(0, $"{DateTime.Now:dd MMM HH:mm}  {message}");
        if (notifications.Count > 30) notifications.RemoveAt(notifications.Count - 1);
        Console.WriteLine($"[{Name}] {message}");
    }

    public override string Describe() => $"{Name} ({Role})";
}

public class Customer : User, IObserver
{
    private readonly List<Order> orders = new();
    private readonly List<string> notifications = new();

    public string Address { get; }
    public string Phone { get; }
    public int WarningCount { get; private set; }
    public bool IsBlacklisted { get; private set; }
    public IReadOnlyList<Order> Orders => orders;
    public IReadOnlyList<string> Notifications => notifications;

    public Customer(int id, string name, string email, string address, string phone)
        : base(id, name, email)
    {
        Address = address;
        Phone = phone;
    }

    public override bool Login(string email) =>
        string.Equals(email?.Trim(), Email, StringComparison.OrdinalIgnoreCase);

    public void AddWarning() { if (WarningCount < 3) WarningCount++; }
    public void Blacklist() => IsBlacklisted = true;
    public bool CanBuyOnCredit() => !IsBlacklisted;

    internal void AddOrder(Order order) => orders.Add(order);
    internal void Restore(int warnings, bool blacklisted) { WarningCount = warnings; IsBlacklisted = blacklisted; }

    public void Update(string message)
    {
        notifications.Insert(0, $"{DateTime.Now:dd MMM HH:mm}  {message}");
        if (notifications.Count > 30) notifications.RemoveAt(notifications.Count - 1);
        Console.WriteLine($"[{Name}] {message}");
    }

    public override string Describe() =>
        $"{Name} ({Email}) - warnings {WarningCount}/3{(IsBlacklisted ? ", BLACKLISTED" : "")}";
}
