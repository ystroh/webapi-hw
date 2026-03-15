using Common.Repositories;
using Common.Active;
using myUsers.Models;
using UsersService.interfaces;

namespace myUsers.Services;

public class UsersService : IUsersServices
{
    private readonly IRepository<Users> _repo;
    private readonly IActiveUser _activeUser;

    public UsersService(IRepository<Users> repo, IActiveUser activeUser)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _activeUser = activeUser;
    }

    public List<Users> Get() => _repo.GetAll();

    public Users Get(int id) => _repo.Get(id);

    /// <summary>
    /// יצירת משתמש חדש עם ברירות מחדל למייל ותפקיד במידה ולא סופקו
    /// </summary>
    public Users Create(Users newUsers)
    {
        newUsers.Mail ??= (newUsers.Name ?? "user") + "@example.com";
        newUsers.Role ??= "User";

        return _repo.Create(newUsers);
    }

    /// <summary>
    /// עדכון פרטי משתמש קיים תוך שמירה על נתונים קיימים במידה ולא נשלחו חדשים
    /// </summary>
    public int Update(int id, Users newUsers)
    {
        var existing = _repo.Get(id);
        if (existing == null) return 1;
        if (existing.Id != newUsers.Id) return 2;

        // שמירה על ערכים קיימים במידה והחדשים ריקים
        newUsers.Mail ??= existing.Mail;
        newUsers.Role ??= existing.Role;

        return _repo.Update(id, newUsers);
    }

    public bool Delete(int id) => _repo.Delete(id);
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