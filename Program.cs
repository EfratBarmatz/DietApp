using Microsoft.EntityFrameworkCore;
using Npgsql;
using FitnessPro.Data;

var builder = WebApplication.CreateBuilder(args);

// הגדרות כלליות
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// 1. חיבור למסד הנתונים
string connectionString = "";
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
try {
    if (!string.IsNullOrWhiteSpace(rawUrl)) {
        var uri = new Uri(rawUrl);
        var userInfo = uri.UserInfo.Split(':');
        var builderDb = new NpgsqlConnectionStringBuilder {
            Host = uri.Host, Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo[0], Password = userInfo[1],
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Disable, TrustServerCertificate = true
        };
        connectionString = builderDb.ToString();
    }
} catch { }

builder.Services.AddDbContext<DietDb>(opt => {
    if (!string.IsNullOrEmpty(connectionString)) opt.UseNpgsql(connectionString);
});

// 2. הוספת תמיכה ב-Controllers
builder.Services.AddControllers();
builder.Services.AddCors();

var app = builder.Build();

// 3. הגדרות Middleware
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

// ==========================================
// אזור עדכון מסד הנתונים (התוספת החדשה)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DietDb>();
    try
    {
        // 1. יצירת הטבלאות הבסיסיות אם הן לא קיימות
        db.Database.EnsureCreated();

        // 2. עדכון ידני לטבלת המשתמשים (הוספת העמודות החדשות אם חסרות)
        if (!string.IsNullOrEmpty(connectionString))
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Name"" text DEFAULT 'משתמש/ת';
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Age"" integer DEFAULT 0;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Weight"" double precision DEFAULT 0;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Height"" integer DEFAULT 0;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Gender"" text DEFAULT 'female';
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""ActivityLevel"" double precision DEFAULT 1.2;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""DailyCalorieGoal"" integer DEFAULT 1500;
            ");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB Update Error: " + ex.Message);
    }
}
// ==========================================

app.MapControllers(); 

app.Run();
