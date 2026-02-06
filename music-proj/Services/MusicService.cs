using Microsoft.AspNetCore.Mvc;
using myMusic.Models;
using System.Security.Cryptography.X509Certificates;
using MusicService.interfaces;

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
namespace myMusic.Services;

public class MusicService : IMusicServices
{
  

    private List<Music> list;
     private IWebHostEnvironment  webHost;
     private string filePath ;

    public MusicService(IWebHostEnvironment webHost)
    {
        this.webHost = webHost;
        // this.list = new List<Music>{
        //      new Music { Id = 1, Name = "guittar",IsWoodMade=true},
        //      new Music { Id = 2, Name = "fiddle",IsWoodMade=true},
        //      new Music { Id = 3, Name = "organ",IsWoodMade=true},
        //      new Music { Id = 4, Name = "piano",IsWoodMade=false} 
        // };


           this.filePath = Path.Combine(webHost.ContentRootPath, "Data", "music.json");
            using (var jsonFile = File.OpenText(filePath))
            {
                var content = jsonFile.ReadToEnd();
                list = JsonSerializer.Deserialize<List<Music>>(content,
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

    public List<Music> Get()
    {
        return list;
    }

    private Music find(int id)
    {
        return list.FirstOrDefault(p => p.Id == id);

    }

    public Music Get(int id) => find(id);

    public Music Create(Music newMusic)
    {
        var maxId = list.Max(p => p.Id);
        newMusic.Id = maxId + 1;
        list.Add(newMusic);
         saveToFile();
            return newMusic;
        
    }

    public int Update(int id, Music newMusic)
    {
            var m = find(id);
        if(m == null)
          return 1;

        if(m.Id != newMusic.Id)
           return 2;

        var index = list.IndexOf(m);
        list[index] = newMusic;
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
    public static class MusicExtension{
      public static void AddMusicService(this IServiceCollection services)
        {
            services.AddSingleton<IMusicServices, MusicService>();
            //services.AddScope<IOrderManager, OrderManager>();
            //services.AddTransient<IOrderSender, OrderSenderHttp>();            
        }




}


