using System.Threading.Tasks;
using myUsers.Models;

namespace UsersService.interfaces;

    public interface IUsersServices
    {
     List<Users> Get();
   
     Users Get(int id);
     Users Create(Users newUsers);

     int Update(int id, Users newUsers);
   
     bool Delete(int id);
    }
    
