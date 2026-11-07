using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages.Manage;

public class OverdueModel : AppPage
{
    public List<string> Results { get; set; } = new();
    public DateTime RunDate { get; set; } = DateTime.Today;
    public IEnumerable<Order> CreditOrders => Shop.Orders.Where(o => o.Payment is CreditPayment);
    public string PricingKind => Shop.PricingKind;

    public IActionResult OnGet()
    {
        if (!Can("ProcessOverdue")) return RedirectToPage("/Login");
        return Page();
    }

    // "Date to check" lets you simulate time passing so you can demo warnings and repossession.
    public IActionResult OnPostRun(DateTime? runDate)
    {
        if (!Can("ProcessOverdue")) return RedirectToPage("/Login");
        RunDate = runDate ?? DateTime.Today;
        try { Results = Shop.ProcessOverduePayments(MeStaff!, RunDate); }
        catch (StoreException ex) { Error = ex.Message; return RedirectToPage(); }
        return Page();
    }

    public IActionResult OnPostStrategy(string kind) =>
        Try(() => { Shop.SetPricingStrategy(MeStaff!, kind); return "Resale pricing strategy updated."; });
}
