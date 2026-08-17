using MilkApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Postgrest;
using System.Security.Claims;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillsController : ControllerBase
{
    private const string ImageBucket = "bill-images";

    private readonly Supabase.Client _supabase;

    public BillsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    private Guid CurrentUserId
    {
        get
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token.");
            }
            return userId;
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<BillDto>>> GetAll()
    {
        try
        {
            var userId = CurrentUserId;
            var response = await _supabase.From<MilkDeposit>()
                .Where(d => d.UserId == userId)
                .Order(d => d.DepositedAt, Constants.Ordering.Descending)
                .Get();

            Dictionary<Guid, string> recipientDict = new();
            try
            {
                var recipientResponse = await _supabase.From<Recipient>()
                    .Where(r => r.UserId == userId)
                    .Get();
                recipientDict = recipientResponse.Models
                    .GroupBy(r => r.Id)
                    .ToDictionary(g => g.Key, g => g.First().Name);
            }
            catch (Exception rEx)
            {
                Console.WriteLine($"[BillsController] Warning: Recipient lookup failed: {rEx.Message}");
            }

            var billDtos = response.Models.Select(d =>
            {
                var dto = BillDto.FromModel(d);
                if (d.RecipientId.HasValue && recipientDict.TryGetValue(d.RecipientId.Value, out var name))
                {
                    return dto with { VendorName = name };
                }
                return dto;
            }).ToList();

            return Ok(billDtos);
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
        var userId = CurrentUserId;
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
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
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
        var userId = CurrentUserId;
        await _supabase.From<MilkDeposit>().Where(d => d.Id == id && d.UserId == userId).Delete();
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BillDto>> Update(Guid id, [FromForm] CreateBillRequest request)
    {
        var userId = CurrentUserId;
        var existingResponse = await _supabase.From<MilkDeposit>().Where(d => d.Id == id && d.UserId == userId).Get();
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
