using GeradorDeClientes.Services;
using GeradorDeClientes.Data;
using Microsoft.EntityFrameworkCore;
// Production-ready: removed test endpoints and diagnostic-only usings

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Register");
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// EF Core SQLite DbContext (file in App_Data)
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "app.db");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

// User service (EF-backed)
builder.Services.AddScoped<GeradorDeClientes.Services.IUserService, GeradorDeClientes.Services.EfUserService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure DB created (apply migrations or create schema)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch
    {
        db.Database.EnsureCreated();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// Session deve ser registrada entre Routing e MapRazorPages
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

// Diagnostic endpoint (only enabled when ALLOW_DIAG=true)
var allowDiag = builder.Configuration.GetValue<bool?>("ALLOW_DIAG") ?? false;
if (allowDiag)
{
    app.MapGet("/api/diag", async (IServiceProvider services) =>
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbFile = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "app.db");
            var exists = System.IO.File.Exists(dbFile);
            int? users = null;
            try
            {
                users = await db.Usuarios.CountAsync();
            }
            catch { /* ignore errors reading db */ }

            return Results.Json(new { dbExists = exists, users });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message });
        }
    });
}

app.Run();
