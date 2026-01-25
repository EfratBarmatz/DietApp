using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql; // חובה בשביל פוסטגרס

var builder = WebApplication.CreateBuilder(args);

// --- שלב 1: בניית מחרוזת חיבור חכמה ---
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString = "";

try 
{
    if (string.IsNullOrEmpty(rawConnectionString))
    {
        Console.WriteLine("CRITICAL ERROR: DATABASE_URL is missing!");
    }
    else
    {
        // הפונקציה שממירה את הכתובת של רנדר לפורמט של סי-שארפ
        var uri = new Uri(rawConnectionString);
        var userInfo = uri.UserInfo.Split(':');
        var builderDb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Disable // בגרסה חינמית פנימית לפעמים ה-SSL עושה בעיות
        };
        connectionString = builderDb.ToString();
        Console.WriteLine("Connection String built successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error parsing connection string: {ex.Message}");
}

// --- שלב 2: הגדרת ה-DB ---
builder.Services.AddDbContext<DietDb>(opt => 
    opt.UseNpgsql(connectionString));

builder.Services.AddCors();
var app = builder.Build();

// --- שלב 3: יצירת טבלאות (עם לוגים לשגיאות) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DietDb>();
    try 
    { 
        Console.WriteLine("Attempting to connect to DB...");
        db.Database.EnsureCreated(); 
        Console.WriteLine("DB Connection & Creation Successful!");
    } 
    catch (Exception ex) 
    { 
        Console.WriteLine($"DB ERROR: {ex.Message}"); 
        if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

// --- API Endpoints ---

// בדיקת תקינות מיוחדת - כנס לקישור הזה כדי לראות מה הבעיה
app.MapGet("/api/test-db", async (DietDb db) => {
    try {
        var count = await db.Meals.CountAsync();
        return Results.Ok($"Success! DB is connected. Found {count} meals.");
    } catch (Exception ex) {
        return Results.BadRequest($"Error: {ex.Message} \nInner: {ex.InnerException?.Message}");
    }
});

app.MapGet("/api/meals", async (DietDb db) => 
    await db.Meals.OrderByDescending(m => m.Date).ToListAsync());

app.MapPost("/api/meals", async (DietDb db, [FromBody] Meal meal) => {
    meal.Date = DateTime.Now;
    db.Meals.Add(meal);
    await db.SaveChangesAsync();
    return Results.Ok(meal);
});

app.MapDelete("/api/meals/{id}", async (DietDb db, int id) => {
    var meal = await db.Meals.FindAsync(id);
    if (meal is null) return Results.NotFound();
    db.Meals.Remove(meal);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/stats", async (DietDb db) => {
    var today = DateTime.Today;
    var calories = await db.Meals.Where(m => m.Date >= today).SumAsync(m => m.Calories);
    return Results.Ok(new { DailyCalories = calories });
});

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
