using System.Text.Json;
using myMusic.Models;
using MusicService.interfaces;

namespace myMusic.Services;

public class MusicService : IMusicServices
{
    private List<Music> _list;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public MusicService(IWebHostEnvironment webHost)
    {
        // קביעת נתיב הקובץ וטעינת הנתונים בבניית השירות
        _filePath = Path.Combine(webHost.ContentRootPath, "Data", "music.json");
        
        if (File.Exists(_filePath))
        {
            var content = File.ReadAllText(_filePath);
            _list = JsonSerializer.Deserialize<List<Music>>(content, _options) ?? new List<Music>();
        }
        else
        {
            _list = new List<Music>();
        }
    }

    /// <summary>
    /// שמירת הרשימה המעודכנת מהזיכרון בחזרה לקובץ הדיסק
    /// </summary>
    private void SaveToFile()
    {
        var text = JsonSerializer.Serialize(_list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, text);
    }

    public List<Music> Get() => _list;

    public Music Get(int id) => _list.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// יצירת פריט חדש עם מזהה רץ ושמירה לקובץ
    /// </summary>
    public Music Create(Music newMusic)
    {
        var maxId = _list.Any() ? _list.Max(p => p.Id) : 0;
        newMusic.Id = maxId + 1;
        _list.Add(newMusic);
        SaveToFile();
        return newMusic;
    }

    /// <summary>
    /// עדכון פריט קיים לפי מזהה
    /// </summary>
    public int Update(int id, Music newMusic)
    {
        var existing = Get(id);
        if (existing == null) return 1; // לא נמצא
        if (newMusic.Id != id) return 2; // מזהה לא תואם

        var index = _list.IndexOf(existing);
        _list[index] = newMusic;
        SaveToFile();
        return 3; // הצלחה
    }

    /// <summary>
    /// מחיקת פריט מהרשימה ועדכון הקובץ
    /// </summary>
    public bool Delete(int id)
    {
        var music = Get(id);
        if (music == null) return false;

        _list.Remove(music);
        SaveToFile();
        return true;
    }
}

/// <summary>
/// הרחבה לרישום נוח של שירותי המוזיקה ב-Program.cs
/// </summary>
public static class MusicExtension
{
    public static void AddMusicService(this IServiceCollection services)
    {
        services.AddSingleton<IMusicServices, MusicService>();
    }
}