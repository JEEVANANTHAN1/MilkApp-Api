namespace MilkApp.Api.Controllers;

public class CreateRecipientRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
