using MilkApp.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FarmersController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public FarmersController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<ActionResult<List<Farmer>>> GetAll()
    {
        var response = await _supabase.From<Farmer>().Get();
        return Ok(response.Models);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Farmer>> GetById(Guid id)
    {
        var farmer = await _supabase.From<Farmer>()
            .Where(f => f.Id == id)
            .Single();

        return farmer is null ? NotFound() : Ok(farmer);
    }

    [HttpPost]
    public async Task<ActionResult<Farmer>> Create(Farmer farmer)
    {
        farmer.Id = Guid.NewGuid();
        farmer.CreatedAt = DateTime.UtcNow;

        var response = await _supabase.From<Farmer>().Insert(farmer);
        var created = response.Models.SingleOrDefault();

        return created is null
            ? Problem("Failed to create farmer.")
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Farmer farmer)
    {
        farmer.Id = id;
        await _supabase.From<Farmer>().Update(farmer);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _supabase.From<Farmer>().Where(f => f.Id == id).Delete();
        return NoContent();
    }
}
