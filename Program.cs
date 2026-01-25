using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- לוגיקת חיבור ---
string debugInfo = "Starting...";
string connectionString = "";
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

try 
{
    if (string.IsNullOrWhiteSpace(rawUrl))
    {
        debugInfo += "\nERROR: DATABASE_URL environment variable is null or empty!";
    }
    else
    {
        debugInfo += $"\nFound DATABASE_URL (Length: {rawUrl.Length})";
        
        // ניסיון פענוח
        var uri = new Uri(rawUrl);
        var userInfo = uri.UserInfo.Split(':');
        var builderDb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Disable
        };
        connectionString = builderDb.ToString();
        debugInfo += "\nParsing successful! Connection string ready.";
    }
}
catch (Exception ex)
{
    debugInfo += $"\nParsing FAILED: {ex.Message}";
    connectionString = ""; // Reset on error
}

// הגדרת ה-DB
builder.Services.AddDbContext<DietDb>(opt => 
{
    if (!string.IsNullOrEmpty(connectionString))
        opt.UseNpgsql(connectionString);
});

builder.Services.AddCors();
var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

// --- דף אבחון מיוחד ---
app.MapGet("/api/debug", () => Results.Text(debugInfo));

// --- API רגיל ---
app.MapGet("/api/meals", async (DietDb db) => 
{
    if (string.IsNullOrEmpty(connectionString)) return Results.Problem("Database not configured. Check /api/debug");
    return Results.Ok(await db.Meals.OrderByDescending(m => m.Date).ToListAsync());
});

app.MapPost("/api/meals", async (DietDb db, [FromBody] Meal meal) => {
    if (string.IsNullOrEmpty(connectionString)) return Results.Problem("Database not configured");
    meal.Date = DateTime.Now;
    db.Meals.Add(meal);
    await db.SaveChangesAsync();
    return Results.Ok(meal);
});

app.MapGet("/api/stats", async (DietDb db) => {
    if (string.IsNullOrEmpty(connectionString)) return Results.Problem("Database not configured");
    var today = DateTime.Today;
    var calories = await db.Meals.Where(m => m.Date >= today).SumAsync(m => m.Calories);
    return Results.Ok(new { DailyCalories = calories });
});

// יצירת טבלאות בטוחה
using (var scope = app.Services.CreateScope())
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        var db = scope.ServiceProvider.GetRequiredService<DietDb>();
        try { db.Database.EnsureCreated(); } catch { }
    }
}

app.Run();

public class DietDb : DbContext {
    public DietDb(DbContextOptions<DietDb> options) : base(options) {}
    public DbSet<Meal> Meals { get; set; }
}

public class Meal {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Calories { get; set; }
    public DateTime Date { get; set; }
}
