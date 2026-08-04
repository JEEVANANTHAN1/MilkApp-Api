using Postgrest.Attributes;
using Postgrest.Models;

namespace MilkApp.Api.Models;

[Table("farmers")]
public class Farmer : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("village")]
    public string? Village { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
