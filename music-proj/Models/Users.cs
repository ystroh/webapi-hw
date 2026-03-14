namespace myUsers.Models;

public class Users
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int password { get; set; }
    // added email (mail) and role fields
    public string Mail { get; set; }
    public string Role { get; set; }
}