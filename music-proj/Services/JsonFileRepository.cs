using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Common.Repositories;
using System;

namespace Common.Services
{
    /// <summary>
    /// JsonFileRepository - יישום פשוט של IRepository שמאחסן רשומות בקובץ JSON.
    /// תכונות עיקריות:
    /// - מנוהל כ‑singleton (הרשומות נטענות לזיכרון והכתיבה מתבצעת ל‑file בעת שינוי).
    /// - משתמש ב‑locker (lock) כדי להבטיח thread-safety על קריאה/כתיבה.
    /// - דורש ש‑T יכיל property בשם `Id` מטיפוס int (מנוהל באמצעות reflection בקוד זה).
    /// שימוש:
    /// - ב‑DI יש להרשימו כ‑Singleton ולספק נתיב לקובץ JSON (לדוגמה "Data/users.json").
    /// </summary>
    public class JsonFileRepository<T> : IRepository<T> where T : class
    {
        private readonly string filePath;
        private List<T> items;
        private readonly object locker = new object();

        /// <summary>
        /// בנאי - מקבל IWebHostEnvironment כדי לבנות נתיב יחסית ל‑ContentRootPath
        /// או נתיב מוחלט אם נמסר כזה.
        /// </summary>
        public JsonFileRepository(IWebHostEnvironment env, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                filePath = relativePath;
            else
                filePath = Path.Combine(env.ContentRootPath, relativePath);

            LoadFromFile();
        }

        // טען את הקובץ לזיכרון (או צור חדש אם לא קיים)
        private void LoadFromFile()
        {
            lock (locker)
            {
                if (!File.Exists(filePath))
                {
                    items = new List<T>();
                    SaveToFile();
                    return;
                }

                var content = File.ReadAllText(filePath);
                items = JsonSerializer.Deserialize<List<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<T>();
            }
        }

        // שמור את המצב הנוכחי חזרה לקובץ (מופעל בעת שינויים)
        private void SaveToFile()
        {
            lock (locker)
            {
                var text = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, text);
            }
        }

        // מחזיר עותק של כל הפריטים (כדי למנוע שינוי מחוץ לריפוזיטורי)
        public List<T> GetAll()
        {
            lock (locker) { return items.Select(i => i).ToList(); }
        }

        // מחזיר פריט לפי id
        public T Get(int id)
        {
            lock (locker)
            {
                return items.FirstOrDefault(i => GetId(i) == id);
            }
        }

        // יוצר פריט חדש ומקצה Id אוטומטית
        public T Create(T item)
        {
            lock (locker)
            {
                var max = items.Any() ? items.Max(i => GetId(i)) : 0;
                SetId(item, max + 1);
                items.Add(item);
                SaveToFile();
                return item;
            }
        }

        // עדכון פריט קיים
        public int Update(int id, T item)
        {
            lock (locker)
            {
                var existing = items.FirstOrDefault(i => GetId(i) == id);
                if (existing == null) return 1; // לא נמצא
                if (GetId(item) != id) return 2; // id לא תואם
                var idx = items.IndexOf(existing);
                items[idx] = item;
                SaveToFile();
                return 3; // הצלחה
            }
        }

        // מחיקת פריט לפי id
        public bool Delete(int id)
        {
            lock (locker)
            {
                var existing = items.FirstOrDefault(i => GetId(i) == id);
                if (existing == null) return false;
                items.Remove(existing);
                SaveToFile();
                return true;
            }
        }

        // עזרי reflection לגישה/הגדרה של property בשם Id
        private int GetId(T item)
        {
            var prop = item.GetType().GetProperty("Id");
            if (prop == null) return 0;
            return (int)(prop.GetValue(item) ?? 0);
        }

        private void SetId(T item, int id)
        {
            var prop = item.GetType().GetProperty("Id");
            if (prop == null) return;
            prop.SetValue(item, id);
        }
    }
}
