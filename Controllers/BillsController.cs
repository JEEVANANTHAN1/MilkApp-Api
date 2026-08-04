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
        var response = await _supabase.From<MilkDeposit>()
            .Order(d => d.DepositedAt, Constants.Ordering.Descending)
            .Get();

        return Ok(response.Models.Select(BillDto.FromModel));
    }

    [HttpPost]
    public async Task<ActionResult<BillDto>> Create([FromForm] CreateBillRequest request)
    {
        string? imageUrl = null;
        if (request.Image is { Length: > 0 })
        {
            using var stream = new MemoryStream();
            await request.Image.CopyToAsync(stream);
            var path = $"{Guid.NewGuid()}{Path.GetExtension(request.Image.FileName)}";

            await _supabase.Storage.From(ImageBucket).Upload(stream.ToArray(), path);
            imageUrl = _supabase.Storage.From(ImageBucket).GetPublicUrl(path);
        }

        var deposit = new MilkDeposit
        {
            Id = Guid.NewGuid(),
            QuantityLiters = request.QuantityLiters,
            FatPercentage = request.FatPercent,
            SnfPercentage = request.SnfPercent,
            RatePerLiter = request.RatePerLiter,
            TotalAmount = request.TotalAmount,
            VendorName = request.VendorName,
            MemberCode = request.MemberCode,
            MemberName = request.MemberName,
            Notes = request.Notes,
            ImageUrl = imageUrl,
            DepositedAt = request.BillDate.ToDateTime(TimeOnly.MinValue),
            CreatedAt = DateTime.UtcNow
        };

        var response = await _supabase.From<MilkDeposit>().Insert(deposit);
        var created = response.Models.SingleOrDefault();

        return created is null
            ? Problem("Failed to record bill.")
            : StatusCode(StatusCodes.Status201Created, BillDto.FromModel(created));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _supabase.From<MilkDeposit>().Where(d => d.Id == id).Delete();
        return NoContent();
    }
}
