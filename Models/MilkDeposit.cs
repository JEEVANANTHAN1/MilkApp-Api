using Postgrest.Attributes;
using Postgrest.Models;

namespace MilkApp.Api.Models;

public enum MilkShift
{
    Morning,
    Evening
}

[Table("milk_deposits")]
public class MilkDeposit : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("farmer_id")]
    public Guid? FarmerId { get; set; }

    [Column("quantity_liters")]
    public decimal QuantityLiters { get; set; }

    [Column("fat_percentage")]
    public decimal FatPercentage { get; set; }

    [Column("snf_percentage")]
    public decimal? SnfPercentage { get; set; }

    [Column("rate_per_liter")]
    public decimal RatePerLiter { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("vendor_name")]
    public string? VendorName { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("shift")]
    public string Shift { get; set; } = MilkShift.Morning.ToString();

    [Column("deposited_at")]
    public DateTime DepositedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Reference(typeof(Farmer), useInnerJoin: false)]
    public Farmer? Farmer { get; set; }
}
