using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessPro.Data;
using FitnessPro.Models;

namespace FitnessPro.Controllers;

[Route("api/meals")]
[ApiController]
public class MealsController : ControllerBase
{
    private readonly DietDb _db;

    public MealsController(DietDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Meal>>> GetMeals(int userId)
    {
        if (userId == 0) return Ok(new List<Meal>());
        return await _db.Meals
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Meal>> AddMeal([FromBody] Meal meal)
    {
        if (meal.UserId == 0) return BadRequest("לא מחובר");
        
        // תיקון זמנים בסיסי אם לא נשלח
        if (meal.Date == default) meal.Date = DateTime.UtcNow;
        if (string.IsNullOrEmpty(meal.MealType)) meal.MealType = "snack";

        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();
        return Ok(meal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMeal(int id)
    {
        var meal = await _db.Meals.FindAsync(id);
        if (meal == null) return NotFound();

        _db.Meals.Remove(meal);
        await _db.SaveChangesAsync();
        return Ok();
    }
    
    // נקודת קצה לסטטיסטיקה - אפשר גם בקונטרולר נפרד אבל זה קשור לארוחות
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(int userId)
    {
         if (userId == 0) return Ok(new { DailyCalories = 0 });
         
         var today = DateTime.UtcNow.AddHours(3).Date; 
         var calories = await _db.Meals
            .Where(m => m.UserId == userId && m.Date >= today)
            .SumAsync(m => m.Calories);
            
         return Ok(new { DailyCalories = calories });
    }
}
