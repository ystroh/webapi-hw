using Microsoft.AspNetCore.Mvc;
using myUsers.Models;
using System.Security.Cryptography.X509Certificates;
using myUsers.Services;
using UsersService.interfaces;

namespace myUsers.controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
   // private MusicService service;
    IUsersServices service;
   public UsersController( IUsersServices UsersService)
    {
        this.service = UsersService;
    }

    [HttpGet()]
    public ActionResult<IEnumerable<Users>> Get()
    {
        return service.Get();
    }


    [HttpGet("{id}")]
    public ActionResult<Users> Get(int id)
    {
        var m = service.Get(id);
        if(m==null)
            return NotFound();
        return m;

    }

    [HttpPost]
    public ActionResult Create(Users newUsers)
    {
        var postedUsers = service.Create(newUsers);
      
       return CreatedAtAction(nameof(Create), new { id = postedUsers.Id });
    }

    // private Music find(int id)
    // {
    //      return service.FirstOrDefault(p => p.Id == id);
    // }

    [HttpPut("{id}")]
    public ActionResult Update(int id, Users newUsers)
    {
        var ans= service.Update( id, newUsers);
      
        if(ans==1)
          return NotFound();

        if(ans==2)
           return BadRequest();

       
        return NoContent();

    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var ans= service.Delete(id);
      
        if(ans==false)
            return NotFound();
        return NoContent();

    }
   
 }
