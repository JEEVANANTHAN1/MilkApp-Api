namespace MilkApp.Api.Models;

public record BillDto(
    Guid Id,
    string BillDate,
    string Shift,
    decimal QuantityLiters,
    decimal RatePerLiter,
    decimal TotalAmount,
    Guid? RecipientId,
    decimal FatPercent,
    decimal? SnfPercent,
    string? Notes,
    string? ImageUrl,
    DateTime CreatedAt)
{
    public static BillDto FromModel(MilkDeposit deposit) => new(
        deposit.Id,
        deposit.DepositedAt.ToString("yyyy-MM-dd"),
        deposit.Shift ?? "Morning",
        deposit.QuantityLiters,
        deposit.RatePerLiter,
        deposit.TotalAmount,
        deposit.RecipientId,
        deposit.FatPercentage,
        deposit.SnfPercentage,
        deposit.Notes,
        deposit.ImageUrl,
        deposit.CreatedAt);
}
