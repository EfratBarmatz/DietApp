using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using FitnessPro.Data;
using FitnessPro.Models;

namespace FitnessPro.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly DietDb _db;

    public AuthController(DietDb db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            return BadRequest("אימייל לא תקין");

        if (request.Password.Length < 8)
            return BadRequest("סיסמה קצרה מדי");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("המשתמש כבר קיים");

        CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "נרשמת בהצלחה" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return BadRequest("משתמש לא נמצא");

        if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            return BadRequest("סיסמה שגויה");

        return Ok(new { userId = user.Id, email = user.Email });
    }

    // פונקציות עזר פרטיות (רק לקונטרולר הזה)
    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        using (var hmac = new HMACSHA512(storedSalt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }
    }
}
