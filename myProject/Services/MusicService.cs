using Microsoft.AspNetCore.Mvc;
using myMusic.Models;
using System.Security.Cryptography.X509Certificates;
using MusicService.interfaces;
namespace myMusic.Services;

public class MusicService : IMusicServices
{
  

     private List<Music> list;

    public MusicService()
    {
        this.list = new List<Music>{
             new Music { Id = 1, Name = "guittar",IsWoodMade=true},
             new Music { Id = 2, Name = "fiddle",IsWoodMade=true},
             new Music { Id = 3, Name = "organ",IsWoodMade=true},
             new Music { Id = 4, Name = "piano",IsWoodMade=false} 
        };
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
    public static class MusicExtension{
      public static void AddMusicService(this IServiceCollection services)
        {
            services.AddSingleton<IMusicServices, MusicService>();
            //services.AddScope<IOrderManager, OrderManager>();
            //services.AddTransient<IOrderSender, OrderSenderHttp>();            
        }




}


