using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pustok;
using Pustok.DAL;
using Pustok.Models;
using Pustok.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddDbContext<PustokDbContext>(opt =>
{
    opt.UseSqlServer("Server=tcp:pustok232db1.database.windows.net,1433;Initial Catalog=p232pustok;Persist Security Info=False;User ID=p232pustokadmin;Password=Admin@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
});


builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Lockout.MaxFailedAccessAttempts = 3;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(30);
    opt.User.RequireUniqueEmail= true;
}).AddDefaultTokenProviders().AddEntityFrameworkStores<PustokDbContext>();

//builder.Services.AddSingleton<LayoutService>();
builder.Services.AddScoped<LayoutService>();

//builder.Services.AddTransient<LayoutService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddCookiePolicy(opts =>
{
    opts.OnAppendCookie = ctx =>
    {
        ctx.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(14);
    };
});

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Events.OnRedirectToLogin = opt.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.HttpContext.Request.Path.Value.StartsWith("/manage"))
        {
            var uri = new Uri(context.RedirectUri);
            context.Response.Redirect("/manage/account/login" + uri.Query);
        }
        else
        {
            var uri = new Uri(context.RedirectUri);
            context.Response.Redirect("/account/login" + uri.Query);
        }

        return Task.CompletedTask;
    };
});



var app = builder.Build();

app.UseCookiePolicy();
//app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<PustokHub>("apphub");

app.Run();
