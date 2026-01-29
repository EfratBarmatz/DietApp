namespace FitnessPro.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public string Name { get; set; } = "משתמש/ת";
    public int Age { get; set; }
    public double Weight { get; set; } 
    public int Height { get; set; } 
    public string Gender { get; set; } = "female"; 
    public double ActivityLevel { get; set; } = 1.2; 
    public int DailyCalorieGoal { get; set; } = 1500; 
}
