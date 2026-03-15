using System.Security.Claims;
using Common.Active;
using Common.Repositories;
using myUsers.Models;

namespace Common.Services;

public class ActiveUser : IActiveUser
{
    public int? Id { get; private set; }
    public string? Name { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public ActiveUser(IHttpContextAccessor ctxAccessor, IRepository<Users> usersRepo)
    {
        var user = ctxAccessor.HttpContext?.User;

        // בדיקה ראשונית האם המשתמש מחובר
        if (user?.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            return;
        }

        IsAuthenticated = true;
        
        // חילוץ נתונים בסיסיים מה-Claims
        Name = user.FindFirstValue(ClaimTypes.Name);
        Role = user.FindFirstValue(ClaimTypes.Role);
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // אימות זהות מול מסד הנתונים
        if (int.TryParse(idClaim, out var id))
        {
            var dbUser = usersRepo.Get(id);
            if (dbUser != null)
            {
                Id = id;
                // השלמת פרטים חסרים במידה ולא נמצאו בטוקן
                Name ??= dbUser.Name;
                Role ??= dbUser.Role;
            }
        }
    }

    /// <summary>
    /// בדיקה האם המשתמש מחזיק בתפקיד מסוים בצורה שאינה תלויה ברישיות (Case-insensitive)
    /// </summary>
    public bool IsInRole(string role)
    {
        return IsAuthenticated && string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }
}