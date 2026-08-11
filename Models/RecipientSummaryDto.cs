namespace MilkApp.Api.Models;

public record RecipientSummaryDto(
    Guid RecipientId,
    string RecipientName,
    string Status,
    decimal TotalLiters,
    decimal TotalAmount,
    int TotalDeliveryDays,
    int MorningOnlyDays,
    int EveningOnlyDays,
    int BothSessionsDays,
    List<BillDto> RecentBills);
