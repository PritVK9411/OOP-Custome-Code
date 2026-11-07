using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages.Manage;

public class StockModel : AppPage
{
    public IReadOnlyList<Furniture> Items => Shop.Inventory.Items;
    public int Stock(int id) => Shop.Inventory.GetStock(id);
    public int Threshold => Shop.Inventory.LowStockThreshold;

    public IActionResult OnGet()
    {
        if (!Can("UpdateStock")) return RedirectToPage("/Login");
        return Page();
    }

    public IActionResult OnPostAdd(string type, string name, decimal price, int qty, int a, int b, string? text) =>
        Try(() =>
        {
            var f = Shop.AddFurniture(MeStaff!, type, name, price, qty, a, b, text ?? "");
            return $"Added {f.Describe()}";
        });

    public IActionResult OnPostStock(int id, int qty) =>
        Try(() => { Shop.UpdateStock(MeStaff!, id, qty); return "Stock updated."; });

    public IActionResult OnPostPrice(int id, decimal price) =>
        Try(() => { Shop.ChangePrice(MeStaff!, id, price); return "Price updated."; });
}
