using Microsoft.EntityFrameworkCore;
using FitnessPro.Models; // מחבר אותנו למודלים שיצרנו למעלה

namespace FitnessPro.Data;

public class DietDb : DbContext
{
    public DietDb(DbContextOptions<DietDb> options) : base(options) { }
    
    public DbSet<Meal> Meals { get; set; }
    public DbSet<User> Users { get; set; }
}
