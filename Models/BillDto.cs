namespace MilkApp.Api.Models;

public record BillDto(
    Guid Id,
    string BillDate,
    decimal QuantityLiters,
    decimal RatePerLiter,
    decimal TotalAmount,
    string? VendorName,
    decimal FatPercent,
    decimal? SnfPercent,
    string? MemberCode,
    string? MemberName,
    string? Notes,
    string? ImageUrl,
    DateTime CreatedAt)
{
    public static BillDto FromModel(MilkDeposit deposit) => new(
        deposit.Id,
        deposit.DepositedAt.ToString("yyyy-MM-dd"),
        deposit.QuantityLiters,
        deposit.RatePerLiter,
        deposit.TotalAmount,
        deposit.VendorName,
        deposit.FatPercentage,
        deposit.SnfPercentage,
        deposit.MemberCode,
        deposit.MemberName,
        deposit.Notes,
        deposit.ImageUrl,
        deposit.CreatedAt);
}
