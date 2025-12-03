using Microsoft.AspNetCore.Mvc;
using myUsers.Models;
using System.Security.Cryptography.X509Certificates;
using UsersService.interfaces;
namespace myUsers.Services;

public class UsersService : IUsersServices
{
  

     private List<Users> list;

    public UsersService()
    {
        this.list = new List<Users>{
             new Users { Id = 1, Name = "yehudit",password=1},
             new Users { Id = 2, Name = "chaya",password=2},
             new Users { Id = 3, Name = "elisheva",password=3},
             new Users { Id = 4, Name = "nomi",password=4} 
        };
    }
   
   

    public List<Users> Get()
    {
        return list;
    }

    private Users find(int id)
    {
        return list.FirstOrDefault(p => p.Id == id);

    }

    public Users Get(int id) => find(id);

    public Users Create(Users newUsers)
    {
        var maxId = list.Max(p => p.Id);
        newUsers.Id = maxId + 1;
        list.Add(newUsers);
            return newUsers;
    }

    public int Update(int id, Users newUsers)
    {
            var m = find(id);
        if(m == null)
          return 1;

        if(m.Id != newUsers.Id)
           return 2;

        var index = list.IndexOf(m);
        list[index] = newUsers;

        return 3;
    }

   
    public bool Delete(int id)
    {
         var m= find(id);
        if(m==null)
            return false;
        list.Remove(m);
        return true;
    }
}
    public static class UsersExtension{
      public static void AddUsersService(this IServiceCollection services)
        {
            services.AddSingleton<IUsersServices, UsersService>();
            //services.AddScope<IOrderManager, OrderManager>();
            //services.AddTransient<IOrderSender, OrderSenderHttp>();            
        }




}


