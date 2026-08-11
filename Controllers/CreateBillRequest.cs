using Microsoft.AspNetCore.Mvc;

namespace MilkApp.Api.Controllers;

public class CreateBillRequest
{
    [FromForm(Name = "billDate")]
    public DateOnly BillDate { get; set; }

    [FromForm(Name = "quantityLiters")]
    public decimal QuantityLiters { get; set; }

    [FromForm(Name = "ratePerLiter")]
    public decimal RatePerLiter { get; set; }

    [FromForm(Name = "totalAmount")]
    public decimal TotalAmount { get; set; }

    [FromForm(Name = "vendorName")]
    public string? VendorName { get; set; }

    [FromForm(Name = "recipientId")]
    public Guid? RecipientId { get; set; }

    [FromForm(Name = "fatPercent")]
    public decimal? FatPercent { get; set; }

    [FromForm(Name = "snfPercent")]
    public decimal? SnfPercent { get; set; }

    [FromForm(Name = "memberCode")]
    public string? MemberCode { get; set; }

    [FromForm(Name = "memberName")]
    public string? MemberName { get; set; }

    [FromForm(Name = "notes")]
    public string? Notes { get; set; }

    [FromForm(Name = "shift")]
    public string Shift { get; set; } = "Morning";

    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }
}
