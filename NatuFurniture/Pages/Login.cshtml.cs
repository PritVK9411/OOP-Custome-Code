using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NatuFurniture.Domain;

namespace NatuFurniture.Pages;

public class LoginModel : AppPage
{
    public string? Problem { get; set; }

    public async Task<IActionResult> OnPostStaffAsync(string email, string password) =>
        await SignIn(email, password ?? "");

    public async Task<IActionResult> OnPostCustomerAsync(string email) =>
        await SignIn(email, "");

    private async Task<IActionResult> SignIn(string email, string credential)
    {
        try
        {
            var user = Shop.Login(email, credential);
            var role = user is Staff s ? s.Role.ToString() : "Customer";
            var claims = new List<Claim>
            {
                new("uid", user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Role, role)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            Message = $"Welcome, {user.Name} ({role})";
            return RedirectToPage("/Index");
        }
        catch (StoreException ex)   // AuthenticationException derives from StoreException
        {
            Problem = ex.Message;
            return Page();
        }
    }
}
