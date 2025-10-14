using GeradorDeClientes.Services;
using GeradorDeClientes.Data;
using Microsoft.EntityFrameworkCore;

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

 
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "app.db");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

 
builder.Services.AddScoped<GeradorDeClientes.Services.IUserService, GeradorDeClientes.Services.EfUserService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

 
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

 
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

 
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

// Protected endpoint to list user emails when DIAG_TOKEN is configured
var diagToken = builder.Configuration["DIAG_TOKEN"];
if (!string.IsNullOrEmpty(diagToken))
{
    app.MapGet("/api/users", async (HttpContext http, IServiceProvider services) =>
    {
        if (!http.Request.Headers.TryGetValue("X-DIAG-TOKEN", out var token) || token != diagToken)
        {
            return Results.StatusCode(401);
        }

        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emails = await db.Usuarios.Select(u => u.Email).ToListAsync();
            return Results.Json(new { count = emails.Count, emails });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message });
        }
    });
}

app.Run();
