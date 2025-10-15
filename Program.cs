using GeradorDeClientes.Services;
using GeradorDeClientes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

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
try
{
    Directory.CreateDirectory(dataDir);
}
catch (Exception ex)
{
    // If directory creation fails early, write to console so platform logs capture it.
    Console.WriteLine($"Failed to create App_Data directory '{dataDir}': {ex}");
}

var dbPath = Path.Combine(dataDir, "app.db");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"));

 
builder.Services.AddScoped<GeradorDeClientes.Services.IUserService, GeradorDeClientes.Services.EfUserService>();

// Persist data protection keys to disk so cookie encryption works across restarts/instances in PaaS
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataDir));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        // Make cookie settings more compatible with proxies and HTTPS load balancers
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        options.Cookie.Path = "/";
        // Optional: increase expiry for tests
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

 
// Enhanced startup diagnostics: log paths, verify App_Data writability, and run migrations with detailed logs.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    logger.LogInformation("Application starting. ContentRootPath={ContentRootPath}", builder.Environment.ContentRootPath);
    logger.LogInformation("App_Data directory: {DataDir}", dataDir);
    logger.LogInformation("SQLite DB path: {DbPath}", dbPath);

    // Quick writable check for App_Data
    try
    {
        var testFile = Path.Combine(dataDir, "._write_test");
        System.IO.File.WriteAllText(testFile, DateTime.UtcNow.ToString("o"));
        System.IO.File.Delete(testFile);
        logger.LogInformation("App_Data is writable.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "App_Data directory is not writable or accessible: {DataDir}", dataDir);
    }

    try
    {
        logger.LogInformation("Attempting EF Migrate()...");
        db.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database Migrate() failed, attempting EnsureCreated() as fallback.");
        try
        {
            var created = db.Database.EnsureCreated();
            logger.LogInformation("EnsureCreated() result: {Created}", created);
        }
        catch (Exception ex2)
        {
            logger.LogError(ex2, "EnsureCreated() also failed. Check file permissions and available disk space.");
        }
    }

    // Try to count users to give immediate feedback in logs
    int userCount = 0;
    try
    {
        userCount = await db.Usuarios.CountAsync();
        logger.LogInformation("Usuarios in DB: {Count}", userCount);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to query Usuarios table. DB might be inaccessible or corrupted.");
    }

    // If there are no users and we're running in Production, create a fallback admin user so operator can login
    try
    {
        if (!builder.Environment.IsDevelopment())
        {
            var emergencyEmail = "emergency@admin.local";

            // Prefer explicit password from env var for predictable access in emergency
            var envPwd = Environment.GetEnvironmentVariable("EMERGENCY_ADMIN_PASSWORD");
            string pwdToUse;
            if (!string.IsNullOrEmpty(envPwd))
            {
                pwdToUse = envPwd;
            }
            else
            {
                // Generate a stable-looking temporary password (will be logged) if env var missing
                pwdToUse = "Adm" + Guid.NewGuid().ToString("N").Substring(0, 10) + "!";
            }

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashed = System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pwdToUse))).Replace("-", "").ToLowerInvariant();

            var existing = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == emergencyEmail.ToLower());
            if (existing == null)
            {
                var admin = new GeradorDeClientes.Models.Usuario
                {
                    Email = emergencyEmail,
                    Senha = hashed
                };
                db.Usuarios.Add(admin);
                await db.SaveChangesAsync();
                logger.LogWarning("Emergency admin created: email={Email}", emergencyEmail);
                logger.LogWarning("Emergency admin password: {Pwd}", pwdToUse);
            }
            else
            {
                // Update password if env var is provided
                if (!string.IsNullOrEmpty(envPwd))
                {
                    existing.Senha = hashed;
                    db.Usuarios.Update(existing);
                    await db.SaveChangesAsync();
                    logger.LogWarning("Emergency admin password updated from env var for email={Email}", emergencyEmail);
                }
                else
                {
                    logger.LogInformation("Emergency admin already exists: email={Email}", emergencyEmail);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create fallback admin user.");
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

// Protected endpoint to test login (POST { email, password }) when DIAG_TOKEN is set
if (!string.IsNullOrEmpty(diagToken))
{
    app.MapPost("/api/test-login", async (HttpContext http, IServiceProvider services) =>
    {
        if (!http.Request.Headers.TryGetValue("X-DIAG-TOKEN", out var token) || token != diagToken)
        {
            return Results.StatusCode(401);
        }

        try
        {
            var body = await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, string>>(http.Request.Body);
            if (body == null || !body.TryGetValue("email", out var email) || !body.TryGetValue("password", out var password))
            {
                return Results.Json(new { ok = false, message = "email and password required" });
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var pwd = password.Trim();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return Results.Json(new { ok = false, message = "user not found" });
            }

            // compute SHA256
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashed = System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pwd))).Replace("-", "").ToLowerInvariant();

            if (hashed == user.Senha)
            {
                return Results.Json(new { ok = true, message = "authenticated" });
            }
            else
            {
                return Results.Json(new { ok = false, message = "invalid password" });
            }
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = ex.Message });
        }
    });
}

// Protected endpoint to reset a user's password (POST { email, newPassword }) when DIAG_TOKEN is set
if (!string.IsNullOrEmpty(diagToken))
{
    app.MapPost("/api/reset-password", async (HttpContext http, IServiceProvider services) =>
    {
        if (!http.Request.Headers.TryGetValue("X-DIAG-TOKEN", out var token) || token != diagToken)
        {
            return Results.StatusCode(401);
        }

        try
        {
            var body = await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, string>>(http.Request.Body);
            if (body == null || !body.TryGetValue("email", out var email) || !body.TryGetValue("newPassword", out var newPassword))
            {
                return Results.Json(new { ok = false, message = "email and newPassword required" });
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var pwd = newPassword.Trim();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return Results.Json(new { ok = false, message = "user not found" });
            }

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashed = System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pwd))).Replace("-", "").ToLowerInvariant();

            user.Senha = hashed;
            db.Usuarios.Update(user);
            await db.SaveChangesAsync();

            return Results.Json(new { ok = true, message = "password reset" });
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = ex.Message });
        }
    });
}

app.Run();
