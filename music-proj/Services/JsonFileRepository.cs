using System.Text.Json;
using Common.Repositories;
using Microsoft.AspNetCore.Hosting;

namespace Common.Services;

public class JsonFileRepository<T> : IRepository<T> where T : class
{
    private readonly string _filePath;
    private List<T> _items;
    private readonly object _locker = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public JsonFileRepository(IWebHostEnvironment env, string relativePath)
    {
        // קביעת נתיב הקובץ יחסית לשורש הפרויקט
        _filePath = Path.IsPathRooted(relativePath) 
            ? relativePath 
            : Path.Combine(env.ContentRootPath, relativePath);

        LoadFromFile();
    }

    /// <summary>
    /// טעינת הנתונים מהקובץ לזיכרון בעת אתחול הריפוזיטורי
    /// </summary>
    private void LoadFromFile()
    {
        lock (_locker)
        {
            if (!File.Exists(_filePath))
            {
                _items = new List<T>();
                SaveToFile();
                return;
            }

            var content = File.ReadAllText(_filePath);
            _items = JsonSerializer.Deserialize<List<T>>(content, _jsonOptions) ?? new List<T>();
        }
    }

    /// <summary>
    /// שמירת מצב הרשימה הנוכחי בזיכרון בחזרה לקובץ הפיזי
    /// </summary>
    private void SaveToFile()
    {
        lock (_locker)
        {
            var text = JsonSerializer.Serialize(_items, _jsonOptions);
            File.WriteAllText(_filePath, text);
        }
    }

    public List<T> GetAll()
    {
        lock (_locker) return _items.ToList();
    }

    public T Get(int id)
    {
        lock (_locker) return _items.FirstOrDefault(i => GetId(i) == id);
    }

    /// <summary>
    /// הוספת אובייקט חדש תוך יצירת מזהה ייחודי (Max ID + 1)
    /// </summary>
    public T Create(T item)
    {
        lock (_locker)
        {
            var maxId = _items.Any() ? _items.Max(i => GetId(i)) : 0;
            SetId(item, maxId + 1);
            _items.Add(item);
            SaveToFile();
            return item;
        }
    }

    public int Update(int id, T item)
    {
        lock (_locker)
        {
            var existing = _items.FirstOrDefault(i => GetId(i) == id);
            if (existing == null) return 1; // לא נמצא
            if (GetId(item) != id) return 2; // חוסר התאמה במזהים
            
            var index = _items.IndexOf(existing);
            _items[index] = item;
            SaveToFile();
            return 3; // הצלחה
        }
    }

    public bool Delete(int id)
    {
        lock (_locker)
        {
            var existing = _items.FirstOrDefault(i => GetId(i) == id);
            if (existing == null) return false;
            
            _items.Remove(existing);
            SaveToFile();
            return true;
        }
    }

    // שימוש ב-Reflection כדי לגשת למאפיין ה-Id של טיפוס גנרי שאינו ידוע מראש
    private int GetId(T item)
    {
        var prop = item.GetType().GetProperty("Id");
        return (int)(prop?.GetValue(item) ?? 0);
    }

    private void SetId(T item, int id)
    {
        var prop = item.GetType().GetProperty("Id");
        prop?.SetValue(item, id);
    }
}