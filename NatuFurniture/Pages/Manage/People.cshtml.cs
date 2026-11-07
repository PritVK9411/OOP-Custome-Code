using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages.Manage;

public class PeopleModel : AppPage
{
    public IEnumerable<Customer> Customers => Shop.Customers.OrderBy(c => c.Name);
    public IReadOnlyList<Staff> StaffList => Shop.StaffMembers;

    public IActionResult OnGet()
    {
        if (!Can("RegisterCustomer")) return RedirectToPage("/Login");
        return Page();
    }

    public IActionResult OnPostCustomer(string name, string email, string address, string phone) =>
        Try(() => $"Registered {Shop.RegisterCustomer(MeStaff!, name, email, address ?? "", phone ?? "").Name}.");

    public IActionResult OnPostStaff(string name, string email, string password, string role) =>
        Try(() => { Shop.AddStaff(MeStaff!, name, email, password, role); return $"Added staff member {name}."; });

    public IActionResult OnPostUnlock(int id) =>
        Try(() => { Shop.UnlockStaff(MeStaff!, id); return "Account unlocked."; });
}
