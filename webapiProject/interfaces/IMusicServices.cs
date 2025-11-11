using System.Threading.Tasks;
using myMusic.Models;

namespace MusicService.interfaces;

    public interface IMusicServices
    {
     List<Music> Get();
   
     Music Get(int id);
     Music Create(Music newMusic);

     int Update(int id, Music newMusic);
   
     bool Delete(int id);
    }
    
