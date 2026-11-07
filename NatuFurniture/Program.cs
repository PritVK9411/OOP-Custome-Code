using Microsoft.AspNetCore.Authentication.Cookies;
using NatuFurniture.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.AccessDeniedPath = "/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Loads App_Data/store.json if it exists, otherwise seeds sample data.
Store.Instance.Initialise(Path.Combine(app.Environment.ContentRootPath, "App_Data", "store.json"));

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
