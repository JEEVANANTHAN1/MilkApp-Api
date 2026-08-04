namespace MilkApp.Api.Models;

public record MilkDepositDto(
    Guid Id,
    Guid? FarmerId,
    decimal QuantityLiters,
    decimal FatPercentage,
    decimal RatePerLiter,
    decimal TotalAmount,
    string Shift,
    DateTime DepositedAt,
    DateTime CreatedAt)
{
    public static MilkDepositDto FromModel(MilkDeposit deposit) => new(
        deposit.Id,
        deposit.FarmerId,
        deposit.QuantityLiters,
        deposit.FatPercentage,
        deposit.RatePerLiter,
        deposit.TotalAmount,
        deposit.Shift,
        deposit.DepositedAt,
        deposit.CreatedAt);
}
