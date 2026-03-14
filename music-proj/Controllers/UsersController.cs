using Microsoft.AspNetCore.Mvc;
using myUsers.Models;
using System.Security.Cryptography.X509Certificates;
using myUsers.Services;
using UsersService.interfaces;
using System.Security.Claims;
using TokenServices.Services;
using Microsoft.AspNetCore.Authorization;
using MusicService.interfaces;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
namespace myUsers.controllers;
using Microsoft.AspNetCore.Authentication.Cookies;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{

// [HttpGet("login-google")]
// public IActionResult Login()
// {
//     // זה שולח את המשתמש לגוגל ומבקש ממנו לחזור לדף הבית אחרי האישור
//     return Challenge(new AuthenticationProperties { RedirectUri = "/" }, GoogleDefaults.AuthenticationScheme);
// }
[HttpGet("login-google")]
public IActionResult Login()
{
    // אנחנו שולחים את המשתמש לגוגל, ואומרים לגוגל: כשסיימת, תחזור לכתובת /google-response
    var properties = new AuthenticationProperties { RedirectUri = "/google-response" };
    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}
[HttpGet("/google-response")]
public async Task<IActionResult> GoogleResponse()
{
    // 1. שליפת המידע הזמני שגוגל החזיר (מתוך ה-Cookie הזמני)
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    if (!result.Succeeded) 
        return Redirect("/login.html?error=auth_failed");

    // 2. חילוץ המייל של המשתמש שגוגל אימת
    var email = result.Principal.FindFirstValue(ClaimTypes.Email);

    // 3. אימות מול ה-Service שלך (שקורא מה-users.json)
    // אנחנו מחפשים משתמש שהמייל שלו ב-JSON תואם למייל מגוגל
    var existingUser = service.Get().FirstOrDefault(u => 
        !string.IsNullOrEmpty(u.Mail) && 
        u.Mail.Equals(email, StringComparison.OrdinalIgnoreCase));

    // --- הבדיקה שביקשת: אם המשתמש לא נמצא ב-JSON ---
    if (existingUser == null)
    {
        Console.WriteLine($"DEBUG: Google login failed. Email {email} not found in users.json");
        return Redirect("/login.html?error=user_not_found");
    }

    // 4. בניית ה-Claims - העתק מדויק של הלוגיקה שלך ב-Login הרגיל
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString()),
        new Claim(ClaimTypes.Name, existingUser.Name ?? string.Empty),
        new Claim(ClaimTypes.Email, existingUser.Mail ?? string.Empty),
        new Claim(ClaimTypes.Role, existingUser.Role ?? "User"), // לוקח את ה-Role האמיתי (Admin/User)
    };

    // 5. הנפקת הטוקן באמצעות ה-TokenService שלך
    var tokenObject = TokenService.GetToken(claims);
    string finalToken = TokenService.WriteToken(tokenObject);

    // 6. התנתקות מהקוקי הזמני של גוגל
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // 7. שמירה ב-LocalStorage ומעבר לדף הבית
    // שימי לב: אם ה-JS שלך דורש "Bearer " לפני הטוקן, הוסיפי אותו כאן בתוך הגרשיים
    return Content($@"
        <script>
            localStorage.setItem('Token', '{finalToken}');
            window.location.href = '/index.html';
        </script>", "text/html");
}



   // private MusicService service;
    IUsersServices service;
    MusicService.interfaces.IMusicServices musicService;
   public UsersController( IUsersServices UsersService, MusicService.interfaces.IMusicServices musicService)
    {
        this.service = UsersService;
        this.musicService = musicService;
    }

    [HttpGet()]
    [Authorize(Roles = "Admin")]
    // רק אדמין יכול לראות את כל המשתמשים
    public ActionResult<IEnumerable<Users>> Get()
    {
        return service.Get();
    }


    [HttpGet("{id}")]
    [Authorize]
    // משתמש יכול לראות את עצמו; אדמין יכול לראות כל משתמש
    public ActionResult<Users> Get(int id)
    {
        var m = service.Get(id);
        if(m==null)
            return NotFound();

        // אדמין יכול לראות כל משתמש; משתמש רגיל יכול לראות רק את עצמו
        if (User.IsInRole("Admin") || User.Identity?.Name == m.Name)
            return m;

        return Forbid();

    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    // רק מנהל יכול להוסיף משתמשים חדשים
    public ActionResult Create(Users newUsers)
    {
        // בדיקה שלא קיים משתמש עם אותו שם
        var existing = service.Get().FirstOrDefault(u => u.Name == newUsers.Name);
        if (existing != null)
            return BadRequest("משתמש זה כבר קיים");

        var postedUsers = service.Create(newUsers);
      
       return CreatedAtAction(nameof(Create), new { id = postedUsers.Id });
    }

 

    [HttpPut("{id}")]
    [Authorize]
    // עדכון משתמש: מנהל יכול לעדכן כל משתמש; משתמש יכול לעדכן רק את עצמו
    public ActionResult Update(int id, Users newUsers)
    {
        var existing = service.Get(id);
        if (existing == null)
            return NotFound();

        if (!User.IsInRole("Admin") && User.Identity?.Name != existing.Name)
            return Forbid();

        var ans= service.Update( id, newUsers);
      
        if(ans==1)
          return NotFound();

        if(ans==2)
           return BadRequest();

       
        return NoContent();

    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    // רק אדמין יכול למחוק משתמש - גם את כל המוזיקות שלו
    public ActionResult Delete(int id)
    {
        Console.WriteLine($"DEBUG Delete: Starting delete for user {id}");
        Console.WriteLine($"DEBUG Delete: Headers: {string.Join(", ", Request.Headers.Keys)}");
        Console.WriteLine($"DEBUG Delete: Authorization header: {Request.Headers["Authorization"]}");
        Console.WriteLine($"DEBUG Delete: User.Identity?.IsAuthenticated = {User.Identity?.IsAuthenticated}");
        Console.WriteLine($"DEBUG Delete: User.Identity?.Name = {User.Identity?.Name}");
        Console.WriteLine($"DEBUG Delete: User.Identity?.AuthenticationType = {User.Identity?.AuthenticationType}");
        Console.WriteLine($"DEBUG Delete: User.IsInRole('Admin') = {User.IsInRole("Admin")}");
        
        // הדפס את כל ה-claims
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"DEBUG Delete: Claim - {claim.Type}: {claim.Value}");
        }
        
        // מחיקת כל המוזיקות של המשתמש
        var userMusic = musicService.Get().Where(m => m.UserId == id).ToList();
        foreach (var music in userMusic)
        {
            musicService.Delete(music.Id);
        }

        // מחיקת המשתמש עצמו
        var ans= service.Delete(id);
      
        if(ans==false)
            return NotFound();
        return NoContent();

    }
   

    [HttpPost]
    [Route("[action]")]
    [AllowAnonymous]
    // התחברות — פתוח לכולם
    public ActionResult Login([FromBody] LoginRequest loginRequest)
       {
            try
            {
                if (loginRequest == null)
                {
                    Console.WriteLine("DEBUG: LoginRequest is NULL");
                    return BadRequest("Invalid request");
                }

                Console.WriteLine($"DEBUG: Login attempt - Name: '{loginRequest.Name}', Password: {loginRequest.password}");
                
                // אימות: בדיקה מול מאגר המשתמשים (users.json)
                var allUsers = service.Get();
                Console.WriteLine($"DEBUG: Total users in system: {allUsers.Count()}");
                foreach(var u in allUsers)
                {
                    Console.WriteLine($"DEBUG: User in DB - Name: '{u.Name}', Password: {u.password}");
                }
                
                var existing = allUsers.FirstOrDefault(u => u.Name == loginRequest.Name && u.password == loginRequest.password);
                if (existing == null)
                {
                    Console.WriteLine("DEBUG: No matching user found");
                    return Unauthorized();
                }

                Console.WriteLine($"DEBUG: User found! Id: {existing.Id}");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, existing.Id.ToString()),
                    new Claim(ClaimTypes.Name, existing.Name ?? string.Empty),
                    new Claim(ClaimTypes.Email, existing.Mail ?? string.Empty),
                    new Claim(ClaimTypes.Role, existing.Role ?? "User"),
                };

                var token = TokenService.GetToken(claims);
                return new OkObjectResult(TokenService.WriteToken(token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in Login - {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace - {ex.StackTrace}");
                return BadRequest($"Error: {ex.Message}");
            }
    }

    [HttpGet]
    [Route("me")]
    [Authorize]
    // Returns basic info about the currently authenticated user
    public ActionResult Me()
    {
        try
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var name = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return Ok(new { id, name, role });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in Me - {ex.Message}");
            return BadRequest();
        }
    }

   
 }
