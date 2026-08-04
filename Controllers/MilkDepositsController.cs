using MilkApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Postgrest;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MilkDepositsController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public MilkDepositsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<ActionResult<List<MilkDeposit>>> GetAll(
        [FromQuery] Guid? farmerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        Supabase.Interfaces.ISupabaseTable<MilkDeposit, Supabase.Realtime.RealtimeChannel> query = _supabase.From<MilkDeposit>();

        if (farmerId is not null)
        {
            query = (Supabase.Interfaces.ISupabaseTable<MilkDeposit, Supabase.Realtime.RealtimeChannel>)
                query.Filter(d => d.FarmerId, Constants.Operator.Equals, farmerId.ToString());
        }

        if (from is not null)
        {
            query = (Supabase.Interfaces.ISupabaseTable<MilkDeposit, Supabase.Realtime.RealtimeChannel>)
                query.Filter(d => d.DepositedAt, Constants.Operator.GreaterThanOrEqual, from.Value.ToString("O"));
        }

        if (to is not null)
        {
            query = (Supabase.Interfaces.ISupabaseTable<MilkDeposit, Supabase.Realtime.RealtimeChannel>)
                query.Filter(d => d.DepositedAt, Constants.Operator.LessThanOrEqual, to.Value.ToString("O"));
        }

        var response = await query.Order(d => d.DepositedAt, Constants.Ordering.Descending).Get();
        return Ok(response.Models);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MilkDeposit>> GetById(Guid id)
    {
        var deposit = await _supabase.From<MilkDeposit>()
            .Where(d => d.Id == id)
            .Single();

        return deposit is null ? NotFound() : Ok(deposit);
    }

    [HttpPost]
    public async Task<ActionResult<MilkDeposit>> Create(MilkDeposit deposit)
    {
        deposit.Id = Guid.NewGuid();
        deposit.CreatedAt = DateTime.UtcNow;
        deposit.TotalAmount = Math.Round(deposit.QuantityLiters * deposit.RatePerLiter, 2);

        var response = await _supabase.From<MilkDeposit>().Insert(deposit);
        var created = response.Models.SingleOrDefault();

        return created is null
            ? Problem("Failed to record milk deposit.")
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MilkDeposit deposit)
    {
        deposit.Id = id;
        deposit.TotalAmount = Math.Round(deposit.QuantityLiters * deposit.RatePerLiter, 2);
        await _supabase.From<MilkDeposit>().Update(deposit);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _supabase.From<MilkDeposit>().Where(d => d.Id == id).Delete();
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetDailySummary([FromQuery] DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        var response = await _supabase.From<MilkDeposit>()
            .Filter(d => d.DepositedAt, Constants.Operator.GreaterThanOrEqual, start.ToString("O"))
            .Filter(d => d.DepositedAt, Constants.Operator.LessThan, end.ToString("O"))
            .Get();

        var deposits = response.Models;

        return Ok(new
        {
            Date = start,
            TotalDeposits = deposits.Count,
            TotalQuantityLiters = deposits.Sum(d => d.QuantityLiters),
            TotalAmount = deposits.Sum(d => d.TotalAmount)
        });
    }
}
