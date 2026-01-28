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

// 2. הוספת תמיכה ב-Controllers (זה השינוי החשוב!)
builder.Services.AddControllers();
builder.Services.AddCors();

var app = builder.Build();

// 3. הגדרות Middleware
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers(); // מיפוי אוטומטי של הקבצים שיצרנו

app.Run();
