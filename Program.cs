using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- לוגיקת חיבור (הגרסה המתוקנת) ---
string connectionString = "";
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

try 
{
    if (!string.IsNullOrWhiteSpace(rawUrl))
    {
        var uri = new Uri(rawUrl);
        var userInfo = uri.UserInfo.Split(':');
        
        var builderDb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            // התיקון: אם הפורט הוא -1, נשתמש ב-5432
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Disable
        };
        connectionString = builderDb.ToString();
        Console.WriteLine("DB Connection String created successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error parsing connection string: {ex.Message}");
}

// הגדרת ה-DB
builder.Services.AddDbContext<DietDb>(opt => 
{
    if (!string.IsNullOrEmpty(connectionString))
        opt.UseNpgsql(connectionString);
});

builder.Services.AddCors();
var app = builder.Build();

// יצירת טבלאות אוטומטית בעלייה
using (var scope = app.Services.CreateScope())
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        var db = scope.ServiceProvider.GetRequiredService<DietDb>();
        try { db.Database.EnsureCreated(); } catch { }
    }
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

// --- API Endpoints ---

app.MapGet("/api/meals", async (DietDb db) => 
{
    // אם אין חיבור, נחזיר רשימה ריקה כדי שהאתר לא יקרוס
    if (string.IsNullOrEmpty(connectionString)) return Results.Ok(new List<Meal>());
    return Results.Ok(await db.Meals.OrderByDescending(m => m.Date).ToListAsync());
});

app.MapPost("/api/meals", async (DietDb db, [FromBody] Meal meal) => {
    if (string.IsNullOrEmpty(connectionString)) return Results.Problem("Database Error");
    meal.Date = DateTime.Now;
    db.Meals.Add(meal);
    await db.SaveChangesAsync();
    return Results.Ok(meal);
});

app.MapDelete("/api/meals/{id}", async (DietDb db, int id) => {
    if (string.IsNullOrEmpty(connectionString)) return Results.Problem("Database Error");
    var meal = await db.Meals.FindAsync(id);
    if (meal is null) return Results.NotFound();
    db.Meals.Remove(meal);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/stats", async (DietDb db) => {
    if (string.IsNullOrEmpty(connectionString)) return Results.Ok(new { DailyCalories = 0 });
    var today = DateTime.Today;
    var calories = await db.Meals.Where(m => m.Date >= today).SumAsync(m => m.Calories);
    return Results.Ok(new { DailyCalories = calories });
});

// מסלול בדיקה מהיר
app.MapGet("/api/check", () => string.IsNullOrEmpty(connectionString) ? "DB Error" : "DB OK!");

app.Run();

// --- Models ---
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
