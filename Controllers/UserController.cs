using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessPro.Data;
using FitnessPro.Models;

namespace FitnessPro.Controllers;

[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly DietDb _db;

    public UserController(DietDb db)
    {
        _db = db;
    }

    // קבלת פרטי פרופיל
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // מחזירים רק מה שצריך (בלי סיסמה)
        return Ok(new {
            user.Name, user.Age, user.Weight, user.Height, 
            user.Gender, user.ActivityLevel, user.DailyCalorieGoal
        });
    }

    // עדכון פרופיל וחישוב קלוריות אוטומטי
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserProfileDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // עדכון שדות
        user.Name = dto.Name;
        user.Age = dto.Age;
        user.Weight = dto.Weight;
        user.Height = dto.Height;
        user.Gender = dto.Gender;
        user.ActivityLevel = dto.ActivityLevel;

        // --- חישוב חכם של BMR (נוסחת Mifflin-St Jeor) ---
        if (dto.ManualGoal > 0)
        {
            user.DailyCalorieGoal = dto.ManualGoal; // המשתמש קבע ידנית
        }
        else if (user.Weight > 0 && user.Height > 0 && user.Age > 0)
        {
            double bmr;
            if (user.Gender == "male")
                bmr = (10 * user.Weight) + (6.25 * user.Height) - (5 * user.Age) + 5;
            else
                bmr = (10 * user.Weight) + (6.25 * user.Height) - (5 * user.Age) - 161;

            // הכפלה ברמת הפעילות וחיסור קטן לירידה במשקל (כ-300 קלוריות)
            user.DailyCalorieGoal = (int)(bmr * user.ActivityLevel) - 300;
            
            // הגבלת מינימום בטיחותית
            if (user.DailyCalorieGoal < 1200) user.DailyCalorieGoal = 1200;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "הפרופיל עודכן בהצלחה", newGoal = user.DailyCalorieGoal });
    }
}
