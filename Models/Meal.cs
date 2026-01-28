namespace FitnessPro.Models;

public class Meal
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Calories { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public string MealType { get; set; } // השדה החדש והחשוב
}
