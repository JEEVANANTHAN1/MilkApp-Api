using MilkApp.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipientsController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public RecipientsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecipientDto>>> GetAll()
    {
        var response = await _supabase.From<Recipient>().Get();
        return Ok(response.Models.Select(RecipientDto.FromModel));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecipientDto>> GetById(Guid id)
    {
        var recipient = await _supabase.From<Recipient>()
            .Where(r => r.Id == id)
            .Single();

        return recipient is null ? NotFound() : Ok(RecipientDto.FromModel(recipient));
    }

    [HttpPost]
    public async Task<ActionResult<RecipientDto>> Create([FromBody] Recipient recipient)
    {
        if (recipient.Id == Guid.Empty)
        {
            recipient.Id = Guid.NewGuid();
        }
        recipient.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(recipient.Status))
        {
            recipient.Status = "Active";
        }

        var response = await _supabase.From<Recipient>().Insert(recipient);
        var created = response.Models.SingleOrDefault();

        return created is null
            ? Problem("Failed to create recipient.")
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, RecipientDto.FromModel(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Recipient recipient)
    {
        recipient.Id = id;
        await _supabase.From<Recipient>().Update(recipient);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _supabase.From<Recipient>().Where(r => r.Id == id).Delete();
        return NoContent();
    }
}
