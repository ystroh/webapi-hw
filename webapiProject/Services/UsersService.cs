using Microsoft.AspNetCore.Mvc;
using myUsers.Models;
using System.Security.Cryptography.X509Certificates;
using UsersService.interfaces;


using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
namespace myUsers.Services;

public class UsersService : IUsersServices
{
  

     private List<Users> list;
 private IWebHostEnvironment  webHost;
     private string filePath ;
    public UsersService(IWebHostEnvironment webHost)
    {
        // this.list = new List<Users>{
        //      new Users { Id = 1, Name = "yehudit",password=1},
        //      new Users { Id = 2, Name = "chaya",password=2},
        //      new Users { Id = 3, Name = "elisheva",password=3},
        //      new Users { Id = 4, Name = "nomi",password=4} 
        // };

         this.webHost = webHost;
      


           this.filePath = Path.Combine(webHost.ContentRootPath, "Data", "users.json");
            using (var jsonFile = File.OpenText(filePath))
            {
                var content = jsonFile.ReadToEnd();
                list = JsonSerializer.Deserialize<List<Users>>(content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
    }
   
    private void saveToFile()
        {
            var text = JsonSerializer.Serialize(list);
            File.WriteAllText(filePath, text);
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
        saveToFile();
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
saveToFile();
        return 3;
    }

   
    public bool Delete(int id)
    {
         var m= find(id);
        if(m==null)
            return false;
        list.Remove(m);
        saveToFile();
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


