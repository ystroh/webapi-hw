using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Common.Repositories;
using Common.Active;
using myUsers.Models;
using UsersService.interfaces;

namespace myUsers.Services
{
    // שירות משתמשים שעובד מול IRepository<Users> ושיכול להשתמש ב‑IActiveUser לקבלת מידע על המשתמש הפעיל.
    public class UsersService : IUsersServices
    {
        private readonly IRepository<Users> repo;
        private readonly IActiveUser activeUser;

        public UsersService(IRepository<Users> repo, IActiveUser activeUser)
        {
            this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
            this.activeUser = activeUser; // יכול להיות null במקרים מסוימים
        }

        // מחזיר את כל המשתמשים
        public List<Users> Get()
        {
            return repo.GetAll();
        }

        private Users find(int id)
        {
            return repo.Get(id);
        }

        public Users Get(int id) => find(id);

        public Users Create(Users newUsers)
        {
            if (string.IsNullOrEmpty(newUsers.Mail))
                newUsers.Mail = (newUsers.Name ?? "user") + "@example.com";
            if (string.IsNullOrEmpty(newUsers.Role))
                newUsers.Role = "User";

            return repo.Create(newUsers);
        }

        public int Update(int id, Users newUsers)
        {
            var m = find(id);
            if (m == null)
                return 1;

            if (m.Id != newUsers.Id)
                return 2;

            if (string.IsNullOrEmpty(newUsers.Mail)) newUsers.Mail = m.Mail;
            if (string.IsNullOrEmpty(newUsers.Role)) newUsers.Role = m.Role;

            return repo.Update(id, newUsers);
        }

        public bool Delete(int id)
        {
            return repo.Delete(id);
        }
    }

    public static class UsersExtension
    {
        // רישום פשוט של השירות ב‑DI (scoped)
        public static void AddUsersService(this IServiceCollection services)
        {
            services.AddScoped<IUsersServices, UsersService>();
        }
    }
}


