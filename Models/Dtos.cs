namespace FitnessPro.Models;

public class UserRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    public double Weight { get; set; }
    public int Height { get; set; }
    public string Gender { get; set; }
    public double ActivityLevel { get; set; }
    public int ManualGoal { get; set; }
}
