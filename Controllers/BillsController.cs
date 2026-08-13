using MilkApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Postgrest;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private const string ImageBucket = "bill-images";

    private readonly Supabase.Client _supabase;

    public BillsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<ActionResult<List<BillDto>>> GetAll()
    {
        try
        {
            var response = await _supabase.From<MilkDeposit>()
                .Order(d => d.DepositedAt, Constants.Ordering.Descending)
                .Get();

            return Ok(response.Models.Select(BillDto.FromModel));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BillsController] Warning: GetAll failed: {ex.Message}");
            return Ok(new List<BillDto>());
        }
    }

    [HttpPost]
    public async Task<ActionResult<BillDto>> Create([FromForm] CreateBillRequest request)
    {
        string? imageUrl = null;
        if (request.Image is { Length: > 0 })
        {
            try
            {
                using var stream = new MemoryStream();
                await request.Image.CopyToAsync(stream);
                var path = $"{Guid.NewGuid()}{Path.GetExtension(request.Image.FileName)}";

                await _supabase.Storage.From(ImageBucket).Upload(stream.ToArray(), path);
                imageUrl = _supabase.Storage.From(ImageBucket).GetPublicUrl(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BillsController] Warning: Storage upload failed: {ex.Message}");
            }
        }

        var deposit = new MilkDeposit
        {
            Id = Guid.NewGuid(),
            QuantityLiters = request.QuantityLiters,
            FatPercentage = request.FatPercent ?? 0,
            SnfPercentage = request.SnfPercent,
            RatePerLiter = request.RatePerLiter,
            TotalAmount = request.TotalAmount,
            VendorName = request.VendorName,
            RecipientId = request.RecipientId,
            MemberCode = request.MemberCode,
            MemberName = request.MemberName,
            Notes = request.Notes,
            ImageUrl = imageUrl,
            Shift = request.Shift,
            DepositedAt = request.BillDate.ToDateTime(TimeOnly.MinValue),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var response = await _supabase.From<MilkDeposit>().Insert(deposit);
            var created = response.Models.SingleOrDefault() ?? deposit;
            return StatusCode(StatusCodes.Status201Created, BillDto.FromModel(created));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BillsController] Initial insert failed: {ex.Message}");

            // If insert failed because RecipientId is not found in database recipients table, try inserting with RecipientId = null
            if (deposit.RecipientId is not null)
            {
                try
                {
                    deposit.RecipientId = null;
                    var fallbackResponse = await _supabase.From<MilkDeposit>().Insert(deposit);
                    var createdFallback = fallbackResponse.Models.SingleOrDefault() ?? deposit;
                    return StatusCode(StatusCodes.Status201Created, BillDto.FromModel(createdFallback));
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"[BillsController] Fallback insert failed: {fallbackEx.Message}");
                }
            }

            return Problem($"Failed to record bill: {ex.Message}");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _supabase.From<MilkDeposit>().Where(d => d.Id == id).Delete();
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BillDto>> Update(Guid id, [FromForm] CreateBillRequest request)
    {
        var existingResponse = await _supabase.From<MilkDeposit>().Where(d => d.Id == id).Get();
        var deposit = existingResponse.Models.SingleOrDefault();
        if (deposit is null) return NotFound();

        if (request.Image is { Length: > 0 })
        {
            try
            {
                using var stream = new MemoryStream();
                await request.Image.CopyToAsync(stream);
                var path = $"{Guid.NewGuid()}{Path.GetExtension(request.Image.FileName)}";

                await _supabase.Storage.From(ImageBucket).Upload(stream.ToArray(), path);
                deposit.ImageUrl = _supabase.Storage.From(ImageBucket).GetPublicUrl(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BillsController] Warning: Storage upload failed on update: {ex.Message}");
            }
        }

        deposit.QuantityLiters = request.QuantityLiters;
        deposit.FatPercentage = request.FatPercent ?? 0;
        deposit.SnfPercentage = request.SnfPercent;
        deposit.RatePerLiter = request.RatePerLiter;
        deposit.TotalAmount = request.TotalAmount;
        deposit.VendorName = request.VendorName;
        deposit.RecipientId = request.RecipientId;
        deposit.Shift = request.Shift;
        deposit.Notes = request.Notes;
        deposit.DepositedAt = request.BillDate.ToDateTime(TimeOnly.MinValue);

        try
        {
            await _supabase.From<MilkDeposit>().Update(deposit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BillsController] Warning: Update failed with RecipientId, retrying without RecipientId: {ex.Message}");
            if (deposit.RecipientId is not null)
            {
                deposit.RecipientId = null;
                await _supabase.From<MilkDeposit>().Update(deposit);
            }
        }

        return Ok(BillDto.FromModel(deposit));
    }
}
