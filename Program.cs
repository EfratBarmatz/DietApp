using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// שינוי: קריאת משתנה סביבה כללי
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL");

// שינוי: שימוש ב-PostgreSQL
builder.Services.AddDbContext<DietDb>(opt => 
    opt.UseNpgsql(connectionString));

builder.Services.AddCors();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DietDb>();
    try { db.Database.EnsureCreated(); } catch { }
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/meals", async (DietDb db) => 
    await db.Meals.OrderByDescending(m => m.Date).ToListAsync());

app.MapPost("/api/meals", async (DietDb db, [FromBody] Meal meal) => {
    meal.Date = DateTime.Now;
    db.Meals.Add(meal);
    await db.SaveChangesAsync();
    return Results.Ok(meal);
});

app.MapGet("/api/stats", async (DietDb db) => {
    var today = DateTime.Today;
    var calories = await db.Meals.Where(m => m.Date >= today).SumAsync(m => m.Calories);
    return Results.Ok(new { DailyCalories = calories });
});

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
