using System.Collections.Generic;

namespace Common.Repositories
{
    /// <summary>
    /// IRepository - ממשק גנרי לריפוזיטורי שמנהל איסוף/עדכון/מחיקה של ישויות.
    /// הערות:
    /// - מיועד לשימוש עם יישום שמאחסן את הרשומות בקובץ JSON (ראה JsonFileRepository).
    /// - הריפוזיטורי נרשם כ‑Singleton ב‑DI במערכת זו (מכיל רשימת ישויות בזיכרון ומ persistence לקובץ).
    /// - יישום צריך לטפל בנעילת גישה כדי למנוע race conditions.
    /// </summary>
    public interface IRepository<T>
    {
        // מחזיר עותק של כל הרשומות
        List<T> GetAll();

        // מחזיר רשומה לפי id או null
        T Get(int id);

        // יוצר רשומה חדשה ומחזיר את הרשומה כולל ה‑Id שניתן
        T Create(T item);

        // עדכון; ערך החזרה מסמן: 1=לא נמצא,2=id mismatch,3=אוק
        int Update(int id, T item);

        // מחיקה לפי id; מחזיר true אם נמחקה
        bool Delete(int id);
    }
}
