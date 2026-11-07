using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages;

public class IndexModel : AppPage
{
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    public List<Furniture> Items { get; set; } = new();

    public bool CanOrder => MeCustomer != null || Can("PlaceOrder");
    public bool IsStaffOrderer => MeStaff != null && Can("PlaceOrder");
    public IEnumerable<Customer> CustomerChoices => Shop.Customers.OrderBy(c => c.Name);
    public int Stock(int id) => Shop.Inventory.GetStock(id);

    public void OnGet() => Items = Shop.Inventory.Search(Q ?? "");

    public IActionResult OnPostOrder(int[] ids, string paymentType, int instalments, int? customerId)
    {
        if (Me == null) return RedirectToPage("/Login");

        return Try(() =>
        {
            int cid;
            if (MeCustomer != null) cid = MeCustomer.Id;
            else if (Can("PlaceOrder") && customerId.HasValue) cid = customerId.Value;
            else throw new StoreException("This account cannot place orders.");

            var order = Shop.PlaceOrder(cid, ids ?? Array.Empty<int>(), paymentType, instalments, Me);
            return $"Order #{order.Id} placed - total RM {order.CalculateTotal():N2}. " +
                   (order.Payment is CashPayment ? "Pay in full at the counter." : "Your first instalment is due in one month.");
        }, "/Orders");
    }
}
