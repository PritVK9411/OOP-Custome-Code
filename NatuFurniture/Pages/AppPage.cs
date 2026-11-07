using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NatuFurniture.Domain;
using AppUser = NatuFurniture.Domain.User;

namespace NatuFurniture.Pages;

// Shared base for every page: who is logged in, permission checks, flash messages.
public abstract class AppPage : PageModel
{
    protected static Store Shop => Store.Instance;

    public AppUser? Me
    {
        get
        {
            var v = HttpContext.User.FindFirstValue("uid");
            return int.TryParse(v, out var id) ? Shop.FindUser(id) : null;
        }
    }

    public Staff? MeStaff => Me as Staff;
    public Customer? MeCustomer => Me as Customer;
    public bool Can(string action) => MeStaff?.HasPermission(action) == true;

    // Keys match what _Layout reads from TempData.
    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    // Runs an action; StoreExceptions become readable messages instead of crashes.
    protected IActionResult Try(Func<string> action, string? successPage = null)
    {
        try
        {
            Message = action();
            return successPage == null ? RedirectToPage() : RedirectToPage(successPage);
        }
        catch (StoreException ex)
        {
            Error = ex.Message;
            return RedirectToPage();
        }
    }
}
