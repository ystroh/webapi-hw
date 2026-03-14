using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Common.Active;
using System.Security.Claims;
using myMusic.Models;
using System.Security.Cryptography.X509Certificates;
using myMusic.Services;
using MusicService.interfaces;

namespace myMusic.controllers;

// כל הבקשות ל־Music דורשות Authentication
[ApiController]
[Route("[controller]")]
public class MusicController : ControllerBase
{
   // private MusicService service;
    IMusicServices service;
    private readonly IHubContext<MusicHub> _hubContext;
    private readonly IActiveUser _activeUser;

   public MusicController(IMusicServices musicService, IHubContext<MusicHub> hubContext, IActiveUser activeUser)
    {
        this.service = musicService;
        _hubContext = hubContext;
        _activeUser = activeUser;
    }

    [HttpGet()]
    [Authorize]
    // משתמש רגיל רואה רק את המוזיקה שלו; אדמין רואה הכל
    public ActionResult<IEnumerable<Music>> Get()
    {
        // אם המשתמש מנהל - מחזירים הכל, אחרת מחזירים רק את המוזיקה ששייכת למשתמש הנוכחי
        var isAdmin = User.IsInRole("Admin");
        if (isAdmin)
            return service.Get();

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        var userList = service.Get().Where(m => m.UserId == userId).ToList();
        return userList;
    }


    [HttpGet("{id}")]
    [Authorize]
    // משתמש רגיל רואה רק את המוזיקה שלו; אדמין רואה הכל
    public ActionResult<Music> Get(int id)
    {
        var m = service.Get(id);
        if (m == null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (isAdmin)
            return m;

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (m.UserId != userId)
            return Forbid();

        return m;

    }

    [HttpPost]
    [Authorize]
    // משתמש רגיל יכול ליצור רק פריט במאגר שלו; מנהל יכול ליצור לכל משתמש
    public async Task<ActionResult> Create(Music newMusic)
    {
        var isAdmin = User.IsInRole("Admin");
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (!isAdmin)
        {
            // כופה שהפריט ישויך למשתמש הנוכחי
            newMusic.UserId = userId;
        }

        var postedMusic = service.Create(newMusic);

        // Notify the user's connected SignalR clients about the new item
        if (_activeUser.Id.HasValue)
        {
            var currentUserId = _activeUser.Id.Value.ToString();
            var connections = MusicHub.GetConnections(currentUserId);
            if (connections != null && connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync("ItemUpdated", "create", postedMusic.Id);
            }
        }

        return CreatedAtAction(nameof(Create), new { id = postedMusic.Id });
    }

    // private Music find(int id)
    // {
    //      return service.FirstOrDefault(p => p.Id == id);
    // }

    [HttpPut("{id}")]
    [Authorize]
    // משתמש רגיל יכול לעדכן רק פריטים שלו; מנהל יכול לעדכן הכל
    public async Task<ActionResult> Update(int id, Music newMusic)
    {
        var existing = service.Get(id);
        if (existing == null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (!isAdmin && existing.UserId != userId)
            return Forbid();

        // אם המשתמש רגיל, לוודא שה־UserId נשאר שלו
        if (!isAdmin)
            newMusic.UserId = userId;

        var ans = service.Update(id, newMusic);
        if (ans == 1)
            return NotFound();
        if (ans == 2)
            return BadRequest();

        // Notify the user's connected SignalR clients about the update
        if (_activeUser.Id.HasValue)
        {
            var currentUserId = _activeUser.Id.Value.ToString();
            var connections = MusicHub.GetConnections(currentUserId);
            if (connections != null && connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync("ItemUpdated", "update", id);
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    // משתמש רגיל יכול למחוק רק פריטים שלו; מנהל יכול למחוק הכל
    public async Task<ActionResult> Delete(int id)
    {
        var existing = service.Get(id);
        if (existing == null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (!isAdmin && existing.UserId != userId)
            return Forbid();

        var ans = service.Delete(id);
        if (!ans)
            return NotFound();

        // Notify the user's connected SignalR clients about the deletion
        if (_activeUser.Id.HasValue)
        {
            var currentUserId = _activeUser.Id.Value.ToString();
            var connections = MusicHub.GetConnections(currentUserId);
            if (connections != null && connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync("ItemUpdated", "delete", id);
            }
        }

        return NoContent();

    }
   
 }
