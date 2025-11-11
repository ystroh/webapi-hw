using Microsoft.AspNetCore.Mvc;
using myMusic.Models;
using System.Security.Cryptography.X509Certificates;
using myMusic.Services;

namespace myMusic.controllers;

[ApiController]
[Route("[controller]")]
public class MusicController : ControllerBase
{
    private MusicService service;

   public MusicController()
    {
        service = new MusicService();
    }

    [HttpGet()]
    public ActionResult<IEnumerable<Music>> Get()
    {
        return service.Get();
    }


    [HttpGet("{id}")]
    public ActionResult<Music> Get(int id)
    {
        var m = service.Get(id);
        if(m==null)
            return NotFound();
        return m;

    }

    [HttpPost]
    public ActionResult Create(Music newMusic)
    {
        var postedMusic = service.Create(newMusic);
      
       return CreatedAtAction(nameof(Create), new { id = postedMusic.Id });
    }

    // private Music find(int id)
    // {
    //      return service.FirstOrDefault(p => p.Id == id);
    // }

    [HttpPut("{id}")]
    public ActionResult Update(int id, Music newMusic)
    {
        var ans= service.Update( id, newMusic);
      
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
