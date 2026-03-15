using Common.Repositories;
using Common.Active;
using myUsers.Models;
using UsersService.interfaces;
using MusicService.interfaces;

namespace myUsers.Services;

public class UsersService : IUsersServices
{
    private readonly IRepository<Users> _repo;
    private readonly IActiveUser _activeUser;
    private readonly IMusicServices _musicService;

    public UsersService(IRepository<Users> repo, IActiveUser activeUser, IMusicServices musicService)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _activeUser = activeUser;
        _musicService = musicService;
    }

    public List<Users> Get() => _repo.GetAll();

    public Users Get(int id) => _repo.Get(id);

    /// <summary>
    /// יצירת משתמש חדש עם ברירות מחדל למייל ותפקיד
    /// </summary>
    public Users Create(Users newUsers)
    {
        newUsers.Mail ??= (newUsers.Name ?? "user") + "@example.com";
        newUsers.Role ??= "User";

        return _repo.Create(newUsers);
    }

    /// <summary>
    /// עדכון פרטי משתמש קיים תוך שמירה על ערכי Mail ו-Role מקוריים במידה ולא סופקו חדשים
    /// </summary>
    public int Update(int id, Users newUsers)
    {
        var existing = _repo.Get(id);
        if (existing == null) return 1;
        if (existing.Id != newUsers.Id) return 2;

        newUsers.Mail ??= existing.Mail;
        newUsers.Role ??= existing.Role;

        return _repo.Update(id, newUsers);
    }

    /// <summary>
    /// מחיקת משתמש ומחיקת כל פריטי המוזיקה המקושרים אליו (Cascading Delete)
    /// </summary>
    public bool Delete(int id)
    {
        var userMusic = _musicService.Get().Where(m => m.UserId == id).ToList();
        
        foreach (var music in userMusic)
        {
            _musicService.Delete(music.Id);
        }

        return _repo.Delete(id);
    }
}

/// <summary>
/// הרחבה לרישום שירות המשתמשים במערכת ה-Dependency Injection
/// </summary>
public static class UsersExtension
{
    public static void AddUsersService(this IServiceCollection services)
    {
        services.AddScoped<IUsersServices, UsersService>();
    }
}