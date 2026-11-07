using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages;

public class OrdersModel : AppPage
{
    public List<Order> Orders { get; set; } = new();
    public bool StaffView => MeStaff != null;
    public bool CanTakePayment => Can("TakePayment");

    public IActionResult OnGet()
    {
        if (Me == null) return RedirectToPage("/Login");

        if (MeCustomer != null) Orders = MeCustomer.Orders.OrderByDescending(o => o.Id).ToList();
        else if (Can("ViewOrders")) Orders = Shop.Orders.OrderByDescending(o => o.Id).ToList();
        else return RedirectToPage("/Index");
        return Page();
    }

    public IActionResult OnPostPay(int orderId, decimal amount) =>
        Try(() => Shop.TakePayment(MeStaff!, orderId, amount));

    public IActionResult OnPostDeliver(int orderId) =>
        Try(() => { Shop.MarkDelivered(MeStaff!, orderId); return $"Order #{orderId} marked as delivered."; });
}
