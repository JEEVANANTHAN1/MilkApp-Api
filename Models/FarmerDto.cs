namespace MilkApp.Api.Models;

public record FarmerDto(Guid Id, string Name, string? PhoneNumber, string? Village, DateTime CreatedAt)
{
    public static FarmerDto FromModel(Farmer farmer) =>
        new(farmer.Id, farmer.Name, farmer.PhoneNumber, farmer.Village, farmer.CreatedAt);
}
