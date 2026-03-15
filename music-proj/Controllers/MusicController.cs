using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Common.Active;
using System.Security.Claims;
using myMusic.Models;
using MusicService.interfaces;

namespace myMusic.controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MusicController : ControllerBase
{
    private readonly IMusicServices _musicService;
    private readonly IHubContext<MusicHub> _hubContext;
    private readonly IActiveUser _activeUser;

    public MusicController(IMusicServices musicService, IHubContext<MusicHub> hubContext, IActiveUser activeUser)
    {
        _musicService = musicService;
        _hubContext = hubContext;
        _activeUser = activeUser;
    }

    /// <summary>
    /// שליפת כל הפריטים: אדמין רואה הכל, משתמש רגיל רואה רק את שלו
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<Music>> Get()
    {
        var allMusic = _musicService.Get();
        
        if (User.IsInRole("Admin"))
            return Ok(allMusic);

        var userId = GetCurrentUserId();
        return Ok(allMusic.Where(m => m.UserId == userId));
    }

    /// <summary>
    /// שליפת פריט לפי מזהה עם בדיקת הרשאות גישה
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Music> Get(int id)
    {
        var music = _musicService.Get(id);
        if (music == null) return NotFound();

        if (!User.IsInRole("Admin") && music.UserId != GetCurrentUserId())
            return Forbid();

        return Ok(music);
    }

    /// <summary>
    /// יצירת פריט חדש ושליחת התראה ב-SignalR
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create(Music newMusic)
    {
        if (!User.IsInRole("Admin"))
            newMusic.UserId = GetCurrentUserId();

        var createdMusic = _musicService.Create(newMusic);
        await NotifyClients("create", createdMusic.Id);

        return CreatedAtAction(nameof(Get), new { id = createdMusic.Id }, createdMusic);
    }

    /// <summary>
    /// עדכון פריט קיים (אדמין או בעל הפריט)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Music updatedMusic)
    {
        var existing = _musicService.Get(id);
        if (existing == null) return NotFound();

        if (!User.IsInRole("Admin") && existing.UserId != GetCurrentUserId())
            return Forbid();

        if (!User.IsInRole("Admin"))
            updatedMusic.UserId = GetCurrentUserId();

        var result = _musicService.Update(id, updatedMusic);
        if (result == 1) return NotFound();
        if (result == 2) return BadRequest();

        await NotifyClients("update", id);
        return NoContent();
    }

    /// <summary>
    /// מחיקת פריט ושליחת התראה לכל החיבורים הפעילים של המשתמש
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = _musicService.Get(id);
        if (existing == null) return NotFound();

        if (!User.IsInRole("Admin") && existing.UserId != GetCurrentUserId())
            return Forbid();

        if (!_musicService.Delete(id)) return NotFound();

        await NotifyClients("delete", id);
        return NoContent();
    }

    // מתודות עזר פרטיות לניקוי הקוד
    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    private async Task NotifyClients(string action, int itemId)
    {
        if (_activeUser.Id.HasValue)
        {
            var connections = MusicHub.GetConnections(_activeUser.Id.Value.ToString());
            if (connections != null && connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync("ItemUpdated", action, itemId);
            }
        }
    }
}