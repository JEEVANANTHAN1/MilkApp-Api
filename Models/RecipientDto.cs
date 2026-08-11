namespace MilkApp.Api.Models;

public record RecipientDto(
    Guid Id,
    string Name,
    string Status,
    DateTime CreatedAt)
{
    public static RecipientDto FromModel(Recipient recipient) => new(
        recipient.Id,
        recipient.Name,
        recipient.Status ?? "Active",
        recipient.CreatedAt);
}
