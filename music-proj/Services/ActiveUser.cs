using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Common.Active;
using Common.Repositories;
using myUsers.Models;

namespace Common.Services
{
    /// <summary>
    /// ActiveUser - מימוש של IActiveUser.
    /// - קורא את ה‑Claims מתוך ה‑HttpContext של הבקשה הנוכחית.
    /// - מאמת (אופציונלי) את ה‑Id מול ה‑Users repository כדי לוודא שהמשתמש קיים.
    /// - נרשם כ‑scoped ב‑DI ולכן זמין לכל שירות שמורשם בטווח הבקשה.
    /// </summary>
    public class ActiveUser : IActiveUser
    {
        public int? Id { get; private set; }
        public string? Name { get; private set; }
        public string? Role { get; private set; }
        public bool IsAuthenticated { get; private set; }

        /// <summary>
        /// בנאי: משתמש ב‑IHttpContextAccessor לקריאת המשתמש הפעיל ו‑IRepository<Users> לאימות זהות במידת הצורך.
        /// </summary>
        public ActiveUser(IHttpContextAccessor ctxAccessor, IRepository<Users> usersRepo)
        {
            var user = ctxAccessor.HttpContext?.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                IsAuthenticated = false;
                return;
            }

            IsAuthenticated = true;
            Name = user.FindFirst(ClaimTypes.Name)?.Value;
            Role = user.FindFirst(ClaimTypes.Role)?.Value;

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id))
            {
                // אימות מול מאגר המשתמשים — אם המשתמש לא קיים נניח שה‑Id איננו תקין
                var u = usersRepo.Get(id);
                if (u != null)
                {
                    Id = id;
                    // העדפת ערכי ה‑Claims; אם הם חסרים נעתיק מהמאגר
                    if (string.IsNullOrEmpty(Role)) Role = u.Role;
                    if (string.IsNullOrEmpty(Name)) Name = u.Name;
                }
            }
        }

        public bool IsInRole(string role)
        {
            if (!IsAuthenticated) return false;
            return string.Equals(Role, role, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
