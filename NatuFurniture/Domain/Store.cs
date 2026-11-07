using System.Text.Json;

namespace NatuFurniture.Domain;

// Singleton: one store per program run.
public sealed class Store
{
    private static readonly Lazy<Store> lazy = new(() => new Store());
    public static Store Instance => lazy.Value;

    public readonly object Sync = new();
    private readonly List<Staff> staff = new();
    private readonly Dictionary<int, Customer> customers = new();
    private readonly List<Order> orders = new();
    private int nextUserId = 1, nextFurnitureId = 1, nextOrderId = 1001, nextPaymentId = 1;
    private string? savePath;
    private string pricingKind = "flat";

    public Inventory Inventory { get; private set; } = new();
    public IPricingStrategy PricingStrategy { get; private set; } = new FlatDiscountStrategy(50);
    public string PricingKind => pricingKind;
    public IReadOnlyList<Staff> StaffMembers => staff;
    public IEnumerable<Customer> Customers => customers.Values;
    public IReadOnlyList<Order> Orders => orders;

    private Store() { }

    // ---------- startup ----------
    public void Initialise(string path)
    {
        savePath = path;
        if (File.Exists(path)) Load(path);
        else { Seed(); Persist(); }
    }

    private void Persist()
    {
        if (savePath == null) return;
        try { Save(savePath); } catch (Exception ex) { Console.WriteLine("Could not save: " + ex.Message); }
    }

    private void Seed()
    {
        AddStaffInternal("Prit", "natuenterprise@store.com", "Natu@2026", StaffRole.Manager);
        AddStaffInternal("Sara", "sales@store.com", "Sales@2026", StaffRole.Sales);
        AddStaffInternal("Wei", "warehouse@store.com", "Store@2026", StaffRole.Warehouse);

        AddCustomerInternal("Thanish", "thanish@mail.com", "12 Jalan Bunga, Melaka", "012-3456789");
        AddCustomerInternal("Kevin", "kevin@mail.com", "7 Taman Seri, Melaka", "013-9988776");

        AddFurnitureInternal("sofa", "Nordic", 899m, 5, 3, 0, "fabric");
        AddFurnitureInternal("sofa", "Chesterfield", 1450m, 2, 2, 0, "leather");
        AddFurnitureInternal("table", "Oak Dining", 650m, 4, 180, 90, "");
        AddFurnitureInternal("bed", "Harbour", 780m, 4, 0, 0, "Queen");
        AddFurnitureInternal("bed", "Cloud", 560m, 6, 0, 0, "Double");
        AddFurnitureInternal("wardrobe", "Oak", 420m, 5, 3, 0, "");
    }

    // ---------- lookups ----------
    public User? FindUser(int id)
    {
        lock (Sync)
        {
            var s = staff.FirstOrDefault(x => x.Id == id);
            if (s != null) return s;
            return customers.TryGetValue(id, out var c) ? c : null;
        }
    }

    public Order? FindOrder(int id) { lock (Sync) return orders.FirstOrDefault(o => o.Id == id); }

    private static void Require(Staff? by, string action)
    {
        if (by == null || !by.HasPermission(action))
            throw new StoreException("You do not have permission to do that.");
    }

    // ---------- login ----------
    public User Login(string email, string credential)
    {
        email = (email ?? "").Trim();
        lock (Sync)
        {
            try
            {
                var s = staff.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (s != null)
                {
                    s.Login(credential ?? "");
                    AuditLog.Instance.Log(s, "Logged in");
                    return s;
                }

                var c = customers.Values.FirstOrDefault(x => x.Login(email));
                if (c != null) return c;

                throw new AuthenticationException("No account found with that email.");
            }
            finally { Persist(); }
        }
    }

    // ---------- people ----------
    private Staff AddStaffInternal(string name, string email, string password, StaffRole role)
    {
        var s = new Staff(nextUserId++, name, email, password, role);
        staff.Add(s);
        if (role != StaffRole.Sales) Inventory.Attach(s); // warehouse + manager get low-stock alerts
        return s;
    }

    private Customer AddCustomerInternal(string name, string email, string address, string phone)
    {
        if (staff.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
            customers.Values.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new StoreException("That email is already registered.");

        var c = (Customer)UserFactory.Create("customer", nextUserId++, name, email, "", address, phone);
        customers[c.Id] = c;
        return c;
    }

    public Customer RegisterCustomer(Staff by, string name, string email, string address, string phone)
    {
        Require(by, "RegisterCustomer");
        lock (Sync)
        {
            var c = AddCustomerInternal(name, email, address, phone);
            AuditLog.Instance.Log(by, $"Registered customer {c.Name}");
            Persist();
            return c;
        }
    }

    public void AddStaff(Staff by, string name, string email, string password, string role)
    {
        Require(by, "ManageStaff");
        lock (Sync)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new StoreException("Password must be at least 6 characters.");
            if (staff.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                customers.Values.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                throw new StoreException("That email is already registered.");

            var created = (Staff)UserFactory.Create(role, nextUserId++, name, email, password);
            staff.Add(created);
            if (created.Role != StaffRole.Sales) Inventory.Attach(created);
            AuditLog.Instance.Log(by, $"Added staff {created.Describe()}");
            Persist();
        }
    }

    public void UnlockStaff(Staff by, int staffId)
    {
        Require(by, "ManageStaff");
        lock (Sync)
        {
            var s = staff.FirstOrDefault(x => x.Id == staffId) ?? throw new StoreException("Staff not found.");
            s.Unlock();
            AuditLog.Instance.Log(by, $"Unlocked account of {s.Name}");
            Persist();
        }
    }

    // ---------- furniture ----------
    private Furniture AddFurnitureInternal(string type, string name, decimal price, int qty, int a, int b, string text)
    {
        var f = FurnitureFactory.Create(type, nextFurnitureId++, name, price, a, b, text);
        Inventory.AddItem(f, qty);
        return f;
    }

    public Furniture AddFurniture(Staff by, string type, string name, decimal price, int qty, int a, int b, string text)
    {
        Require(by, "AddFurniture");
        lock (Sync)
        {
            if (qty < 0) throw new StoreException("Quantity cannot be negative.");
            var f = AddFurnitureInternal(type, name, price, qty, a, b, text);
            AuditLog.Instance.Log(by, $"Added furniture: {f.Describe()} x{qty}");
            Persist();
            return f;
        }
    }

    public void UpdateStock(Staff by, int furnitureId, int quantity)
    {
        Require(by, "UpdateStock");
        lock (Sync)
        {
            Inventory.SetStock(furnitureId, quantity);
            AuditLog.Instance.Log(by, $"Set stock of item #{furnitureId} to {quantity}");
            Persist();
        }
    }

    public void ChangePrice(Staff by, int furnitureId, decimal price)
    {
        Require(by, "ChangePrice");
        lock (Sync)
        {
            if (price <= 0) throw new StoreException("Price must be greater than zero.");
            var f = Inventory.Find(furnitureId) ?? throw new StoreException("Furniture item not found.");
            f.BasePrice = price;
            if (f.Condition == FurnitureCondition.New) f.CurrentPrice = price;
            AuditLog.Instance.Log(by, $"Changed base price of {f.Name} (ID {f.Id}) to RM {price:N2}");
            Persist();
        }
    }

    public void SetPricingStrategy(Staff by, string kind)
    {
        Require(by, "ChangePrice");
        lock (Sync)
        {
            pricingKind = kind == "age" ? "age" : "flat";
            PricingStrategy = pricingKind == "age" ? new AgeDepreciationStrategy() : new FlatDiscountStrategy(50);
            AuditLog.Instance.Log(by, $"Resale pricing strategy set to {pricingKind}");
            Persist();
        }
    }

    // ---------- orders ----------
    public Order PlaceOrder(int customerId, int[] furnitureIds, string paymentType, int instalments = 6, User? actor = null)
    {
        lock (Sync)
        {
            if (!customers.TryGetValue(customerId, out var customer))
                throw new StoreException("Customer not found.");
            if (furnitureIds == null || furnitureIds.Length == 0)
                throw new StoreException("Select at least one item.");

            bool credit = string.Equals(paymentType, "credit", StringComparison.OrdinalIgnoreCase);
            if (credit && !customer.CanBuyOnCredit())
                throw new StoreException("This customer is blacklisted and cannot buy on credit.");
            if (credit && (instalments < 2 || instalments > 12))
                throw new StoreException("Choose between 2 and 12 instalments.");

            foreach (var g in furnitureIds.GroupBy(x => x))
            {
                var item = Inventory.Find(g.Key) ?? throw new StoreException("Furniture item not found.");
                if (item.Status != FurnitureStatus.InStock || Inventory.GetStock(g.Key) < g.Count())
                    throw new OutOfStockException(item.Name);
            }

            var order = new Order(nextOrderId, customer);
            var units = new List<(int catalogueId, Furniture unit)>();
            foreach (var id in furnitureIds)
            {
                var unit = Inventory.Find(id)!.CloneUnit(nextFurnitureId++);
                order.AddItem(unit);
                units.Add((id, unit));
            }

            var total = order.CalculateTotal();
            Payment payment = credit
                ? new CreditPayment(nextPaymentId, total, instalments, DateTime.Today.AddMonths(1))
                : new CashPayment(nextPaymentId, total);
            order.SetPayment(payment);

            foreach (var (catalogueId, unit) in units)
            {
                Inventory.Reserve(catalogueId);
                unit.Status = FurnitureStatus.Reserved;
            }

            nextOrderId++;
            nextPaymentId++;
            orders.Add(order);
            customer.AddOrder(order);
            AuditLog.Instance.Log(actor, $"Placed order #{order.Id} for {customer.Name} ({(credit ? "credit" : "cash")}, RM {total:N2})");
            Persist();
            return order;
        }
    }

    public string TakePayment(Staff by, int orderId, decimal amount)
    {
        Require(by, "TakePayment");
        lock (Sync)
        {
            var order = FindOrder(orderId) ?? throw new StoreException("Order not found.");
            if (order.Status == OrderStatus.Cancelled) throw new StoreException("This order was cancelled.");

            if (!order.Payment.Process(amount))
                throw new StoreException(order.Payment is CashPayment
                    ? "Cash must be paid in full at the time of purchase."
                    : "Payment rejected: amount is below the instalment due, or the plan is closed.");

            if (order.Payment.Status == PaymentStatus.Paid)
            {
                if (order.Status == OrderStatus.Placed) order.Status = OrderStatus.Paid;
                foreach (var item in order.Items) item.Status = FurnitureStatus.Sold;
            }

            AuditLog.Instance.Log(by, $"Took RM {amount:N2} for order #{order.Id}");
            Persist();

            return order.Payment is CashPayment cash
                ? $"Order #{order.Id} paid in full. Change due: RM {cash.ChangeDue:N2}."
                : $"Instalment recorded for order #{order.Id}. Outstanding: RM {order.Payment.Outstanding:N2}.";
        }
    }

    public void MarkDelivered(Staff by, int orderId)
    {
        Require(by, "TakePayment");
        lock (Sync)
        {
            var order = FindOrder(orderId) ?? throw new StoreException("Order not found.");
            if (order.Status == OrderStatus.Cancelled) throw new StoreException("This order was cancelled.");
            order.Status = OrderStatus.Delivered;
            AuditLog.Instance.Log(by, $"Marked order #{order.Id} delivered");
            Persist();
        }
    }

    public List<string> ProcessOverduePayments(Staff by, DateTime today)
    {
        Require(by, "ProcessOverdue");
        var log = new List<string> { $"Checking payments as of {today:dd MMM yyyy}" };

        lock (Sync)
        {
            foreach (var order in orders.ToList())
            {
                if (order.Payment is not CreditPayment cp) continue;

                var before = cp.State.Name;
                var warningsBefore = order.Customer.WarningCount;
                cp.CheckDue(today);
                var after = cp.State.Name;
                if (before == after) continue;

                log.Add($"Order #{order.Id} ({order.Customer.Name}): {before} -> {after}");
                if (order.Customer.WarningCount > warningsBefore)
                    log.Add($"   Warning {order.Customer.WarningCount} of 3 sent to {order.Customer.Name}.");

                if (cp.State is RepossessedState)
                {
                    foreach (var item in order.Items)
                        log.Add($"   Repossessed: {item.Name} (ID {item.Id}) -> now SECOND_HAND at RM {item.CurrentPrice:N2}");
                    log.Add($"   {order.Customer.Name} blacklisted from credit purchases.");
                }
            }

            if (log.Count == 1) log.Add("Nothing to update - no payments changed state.");
            AuditLog.Instance.Log(by, $"Ran overdue payment check for {today:yyyy-MM-dd}");
            Persist();
        }
        return log;
    }

    // ---------- report helpers ----------
    public decimal TotalCollected => orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Payment.AmountPaid);
    public IEnumerable<Order> OverdueOrders => orders.Where(o => o.Payment.Status == PaymentStatus.Overdue);
    public IEnumerable<Furniture> RepossessedItems => Inventory.Items.Where(i => i.Condition == FurnitureCondition.SecondHand);

    // ---------- save / load (JSON) ----------
    private static FurnitureDto ToDto(Furniture f, int stock)
    {
        int a = 0, b = 0; string s = "";
        switch (f)
        {
            case Sofa so: a = so.Seats; s = so.Material; break;
            case Table t: a = t.LengthCm; b = t.WidthCm; break;
            case Bed be: s = be.Size; break;
            case Wardrobe w: a = w.Doors; break;
        }
        return new FurnitureDto(f.Id, f.TypeName, f.Name, f.BasePrice, f.CurrentPrice, f.Condition, f.Status, f.AddedOn, stock, a, b, s);
    }

    private static Furniture FromDto(FurnitureDto d)
    {
        var f = FurnitureFactory.Create(d.Type, d.Id, d.Name, d.BasePrice, d.A, d.B, d.S);
        f.RestoreState(d.Condition, d.Status, d.CurrentPrice, d.AddedOn);
        return f;
    }

    private static PaymentDto ToDto(Payment p) => p switch
    {
        CashPayment c => new PaymentDto(c.Id, "cash", c.TotalAmount, c.AmountPaid, c.Status, c.ChangeDue, 0, 0, default, default, 0, ""),
        CreditPayment cr => new PaymentDto(cr.Id, "credit", cr.TotalAmount, cr.AmountPaid, cr.Status, 0, cr.Instalments,
            cr.InstalmentsPaid, cr.NextDueDate, cr.LastActionDate, cr.LateFeesCharged, cr.State.Name),
        _ => throw new InvalidOperationException("Unknown payment type")
    };

    public void Save(string path)
    {
        lock (Sync)
        {
            var snap = new SnapshotDto(
                staff.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.PasswordHash, s.Role, s.FailedLogins)).ToList(),
                customers.Values.Select(c => new CustomerDto(c.Id, c.Name, c.Email, c.Address, c.Phone, c.WarningCount, c.IsBlacklisted)).ToList(),
                Inventory.Items.Select(f => ToDto(f, Inventory.GetStock(f.Id))).ToList(),
                orders.Select(o => new OrderDto(o.Id, o.Customer.Id, o.Status, o.PlacedOn,
                    o.Items.Select(i => ToDto(i, 0)).ToList(), ToDto(o.Payment))).ToList(),
                pricingKind);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public void Load(string path)
    {
        lock (Sync)
        {
            var snap = JsonSerializer.Deserialize<SnapshotDto>(File.ReadAllText(path))
                       ?? throw new StoreException("Save file is empty.");

            staff.Clear(); customers.Clear(); orders.Clear();
            Inventory = new Inventory();

            foreach (var d in snap.Staff)
            {
                var s = new Staff(d.Id, d.Name, d.Email, d.PasswordHash, d.Role, alreadyHashed: true);
                s.RestoreFailedLogins(d.FailedLogins);
                staff.Add(s);
                if (s.Role != StaffRole.Sales) Inventory.Attach(s);
            }

            foreach (var d in snap.Customers)
            {
                var c = new Customer(d.Id, d.Name, d.Email, d.Address, d.Phone);
                c.Restore(d.Warnings, d.Blacklisted);
                customers[c.Id] = c;
            }

            foreach (var d in snap.Catalogue)
            {
                var f = FromDto(d);
                Inventory.AddItem(f, d.Stock);
                f.RestoreState(d.Condition, d.Status, d.CurrentPrice, d.AddedOn);
            }

            foreach (var d in snap.Orders)
            {
                var order = new Order(d.Id, customers[d.CustomerId]) { Status = d.Status, PlacedOn = d.PlacedOn };
                foreach (var i in d.Items) order.RestoreItem(FromDto(i));

                var pd = d.Payment;
                if (pd.Kind == "credit")
                {
                    var cp = new CreditPayment(pd.Id, pd.Total, pd.Instalments, pd.NextDue);
                    cp.Restore(pd.Total, pd.Paid, pd.Status, pd.InstalmentsPaid, pd.NextDue, pd.LastAction,
                        pd.LateFees, PaymentStates.Parse(pd.StateName));
                    order.AttachPayment(cp);
                }
                else
                {
                    var cash = new CashPayment(pd.Id, pd.Total);
                    cash.Restore(pd.Paid, pd.Status, pd.ChangeDue);
                    order.AttachPayment(cash);
                }
                orders.Add(order);
                customers[d.CustomerId].AddOrder(order);
            }

            pricingKind = snap.Pricing == "age" ? "age" : "flat";
            PricingStrategy = pricingKind == "age" ? new AgeDepreciationStrategy() : new FlatDiscountStrategy(50);

            nextUserId = Math.Max(staff.Select(s => s.Id).DefaultIfEmpty(0).Max(), customers.Keys.DefaultIfEmpty(0).Max()) + 1;
            nextFurnitureId = Math.Max(Inventory.Items.Select(i => i.Id).DefaultIfEmpty(0).Max(),
                orders.SelectMany(o => o.Items).Select(i => i.Id).DefaultIfEmpty(0).Max()) + 1;
            nextOrderId = orders.Select(o => o.Id).DefaultIfEmpty(1000).Max() + 1;
            nextPaymentId = orders.Select(o => o.Payment.Id).DefaultIfEmpty(0).Max() + 1;
        }
    }
}
