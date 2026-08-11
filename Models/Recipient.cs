using Postgrest.Attributes;
using Postgrest.Models;

namespace MilkApp.Api.Models;

[Table("recipients")]
public class Recipient : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Active";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
