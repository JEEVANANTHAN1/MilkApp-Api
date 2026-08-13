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
        try
        {
            var response = await _supabase.From<Recipient>().Get();
            return Ok(response.Models.Select(RecipientDto.FromModel));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] Warning: GetAll failed: {ex.Message}");
            return Ok(new List<RecipientDto>());
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecipientDto>> GetById(Guid id)
    {
        try
        {
            var response = await _supabase.From<Recipient>()
                .Where(r => r.Id == id)
                .Get();

            var recipient = response.Models.SingleOrDefault();
            return recipient is null ? NotFound() : Ok(RecipientDto.FromModel(recipient));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] Warning: GetById failed: {ex.Message}");
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<RecipientDto>> Create([FromBody] CreateRecipientRequest request)
    {
        var recipient = new Recipient
        {
            Id = Guid.NewGuid(),
            Name = request.Name?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var response = await _supabase.From<Recipient>().Insert(recipient);
            var created = response.Models.SingleOrDefault() ?? recipient;
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, RecipientDto.FromModel(created));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] Error creating recipient: {ex.Message}");
            return Problem($"Failed to create recipient: {ex.Message}");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRecipientRequest request)
    {
        var recipient = new Recipient
        {
            Id = id,
            Name = request.Name?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status
        };

        try
        {
            await _supabase.From<Recipient>().Update(recipient);
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] Error updating recipient: {ex.Message}");
            return Problem($"Failed to update recipient: {ex.Message}");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _supabase.From<Recipient>().Where(r => r.Id == id).Delete();
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] Error deleting recipient: {ex.Message}");
            return Problem($"Failed to delete recipient: {ex.Message}");
        }
    }

    [HttpGet("{id:guid}/bills")]
    public async Task<ActionResult<List<BillDto>>> GetBillsForRecipient(Guid id)
    {
        try
        {
            var recipientResponse = await _supabase.From<Recipient>().Where(r => r.Id == id).Get();
            var recipient = recipientResponse.Models.SingleOrDefault();

            var response = await _supabase.From<MilkDeposit>()
                .Order(d => d.DepositedAt, Postgrest.Constants.Ordering.Descending)
                .Get();

            var matches = response.Models.Where(d =>
                d.RecipientId == id ||
                (recipient != null && !string.IsNullOrWhiteSpace(d.VendorName) && d.VendorName.Equals(recipient.Name, StringComparison.OrdinalIgnoreCase))
            );

            return Ok(matches.Select(BillDto.FromModel));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] GetBillsForRecipient error: {ex.Message}");
            return Ok(new List<BillDto>());
        }
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<RecipientSummaryDto>> GetRecipientSummary(Guid id, [FromQuery] string? month)
    {
        try
        {
            var recipientResponse = await _supabase.From<Recipient>().Where(r => r.Id == id).Get();
            var recipient = recipientResponse.Models.SingleOrDefault();

            if (recipient is null) return NotFound();

            var billsResponse = await _supabase.From<MilkDeposit>()
                .Order(d => d.DepositedAt, Postgrest.Constants.Ordering.Descending)
                .Get();

            var bills = billsResponse.Models.Where(d =>
                d.RecipientId == id ||
                (!string.IsNullOrWhiteSpace(d.VendorName) && d.VendorName.Equals(recipient.Name, StringComparison.OrdinalIgnoreCase))
            ).ToList();

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
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipientsController] GetRecipientSummary error: {ex.Message}");
            return Problem($"Failed to compute recipient summary: {ex.Message}");
        }
    }
}
