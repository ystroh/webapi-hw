using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using myUsers.Models;
using UsersService.interfaces;
using MusicService.interfaces;
using TokenServices.Services;

namespace myUsers.controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersServices _userService;
    private readonly IMusicServices _musicService;

    public UsersController(IUsersServices userService, IMusicServices musicService)
    {
        _userService = userService;
        _musicService = musicService;
    }

    /// <summary>
    /// ניתוב המשתמש לאימות מול Google
    /// </summary>
    [HttpGet("login-google")]
    public IActionResult LoginGoogle()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/google-response" }, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// טיפול בתשובה מגוגל והנפקת JWT Token פנימי
    /// </summary>
    [HttpGet("/google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded) return Redirect("/login.html?error=auth_failed");

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var user = _userService.Get().FirstOrDefault(u => u.Mail?.Equals(email, StringComparison.OrdinalIgnoreCase) == true);

        if (user == null) return Redirect("/login.html?error=user_not_found");

        var token = GenerateJwtToken(user);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Content($"<script>localStorage.setItem('Token', '{token}'); window.location.href = '/index.html';</script>", "text/html");
    }

    /// <summary>
    /// התחברות סטנדרטית עם שם וסיסמה
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    public ActionResult Login([FromBody] LoginRequest request)
    {
        var user = _userService.Get().FirstOrDefault(u => u.Name == request.Name && u.password == request.password);
        if (user == null) return Unauthorized();

        return Ok(GenerateJwtToken(user));
    }

    /// <summary>
    /// ניהול משתמשים (אדמין בלבד)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public ActionResult<IEnumerable<Users>> GetAll() => Ok(_userService.Get());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public ActionResult Create(Users newUser)
    {
        if (_userService.Get().Any(u => u.Name == newUser.Name))
            return BadRequest("משתמש זה כבר קיים");

        var created = _userService.Create(newUser);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    /// <summary>
    /// מחיקת משתמש וכל התוכן המשויך אליו (אדמין בלבד)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public ActionResult Delete(int id)
    {
        var userMusic = _musicService.Get().Where(m => m.UserId == id);
        foreach (var music in userMusic) _musicService.Delete(music.Id);

        if (!_userService.Delete(id)) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// שליפת פרטי המשתמש המחובר כעת
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult Me()
    {
        return Ok(new { 
            id = User.FindFirstValue(ClaimTypes.NameIdentifier), 
            name = User.Identity?.Name, 
            role = User.FindFirstValue(ClaimTypes.Role) 
        });
    }

    // יצירת טוקן מאוחד לכל סוגי ההתחברויות
    private string GenerateJwtToken(Users user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name ?? ""),
            new Claim(ClaimTypes.Email, user.Mail ?? ""),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };
        return TokenService.WriteToken(TokenService.GetToken(claims));
    }
}