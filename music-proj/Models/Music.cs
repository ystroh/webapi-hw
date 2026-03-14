namespace myMusic.Models;

public class Music
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsWoodMade { get; set; }
    // מזהה הבעלים של הפריט - מזהה המשתמש שמחזיק את המוזיקה
    public int UserId { get; set; }
}
