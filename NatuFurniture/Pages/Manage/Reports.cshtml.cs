using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages.Manage;

public class ReportsModel : AppPage
{
    public decimal Collected => Shop.TotalCollected;
    public IReadOnlyList<Order> AllOrders => Shop.Orders;
    public IEnumerable<Order> Overdue => Shop.OverdueOrders;
    public IEnumerable<Furniture> Repossessed => Shop.RepossessedItems;
    public IReadOnlyList<string> Audit => AuditLog.Instance.Entries;

    public IActionResult OnGet()
    {
        if (!Can("ViewReports")) return RedirectToPage("/Login");
        return Page();
    }
}
