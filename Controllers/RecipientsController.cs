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

    [HttpGet("{id:guid}/bills")]
    public async Task<ActionResult<List<BillDto>>> GetBillsForRecipient(Guid id)
    {
        var response = await _supabase.From<MilkDeposit>()
            .Where(d => d.RecipientId == id)
            .Order(d => d.DepositedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return Ok(response.Models.Select(BillDto.FromModel));
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<RecipientSummaryDto>> GetRecipientSummary(Guid id, [FromQuery] string? month)
    {
        var recipient = await _supabase.From<Recipient>()
            .Where(r => r.Id == id)
            .Single();

        if (recipient is null) return NotFound();

        var billsResponse = await _supabase.From<MilkDeposit>()
            .Where(d => d.RecipientId == id)
            .Order(d => d.DepositedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        var bills = billsResponse.Models;
        if (!string.IsNullOrWhiteSpace(month))
        {
            bills = bills.Where(b => b.DepositedAt.ToString("yyyy-MM").Equals(month, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var totalLiters = bills.Sum(b => b.QuantityLiters);
        var totalAmount = bills.Sum(b => b.TotalAmount);

        var dateGroups = bills.GroupBy(b => b.DepositedAt.ToString("yyyy-MM-dd")).ToList();
        var totalDeliveryDays = dateGroups.Count;

        int morningOnly = 0, eveningOnly = 0, both = 0;
        foreach (var group in dateGroups)
        {
            var shifts = group.Select(g => g.Shift).ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool hasM = shifts.Contains("Morning");
            bool hasE = shifts.Contains("Evening");
            if (hasM && hasE) both++;
            else if (hasM) morningOnly++;
            else if (hasE) eveningOnly++;
        }

        var dtos = bills.Select(BillDto.FromModel).ToList();

        var summary = new RecipientSummaryDto(
            recipient.Id,
            recipient.Name,
            recipient.Status ?? "Active",
            totalLiters,
            totalAmount,
            totalDeliveryDays,
            morningOnly,
            eveningOnly,
            both,
            dtos);

        return Ok(summary);
    }
}
