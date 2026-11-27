namespace AgriConnect.Shared.Models;

public class Verification
{
    public int Id { get; set; }
    public int SupplyRequestId { get; set; }
    public string ItemName { get; set; } = "";
    public string OfficerName { get; set; } = "";
    public string Decision { get; set; } = "";
    public string Remarks { get; set; } = "";
    public DateTime VerifiedAt { get; set; } = DateTime.Now;
}

public class Advisory
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string District { get; set; } = "";
    public string CropType { get; set; } = "";
    public string Message { get; set; } = "";
    public string OfficerName { get; set; } = "";
    public DateTime PublishedAt { get; set; } = DateTime.Now;
}

public record VerifyRequest(int SupplyRequestId, string FarmerName, string ItemName, string OfficerName, string Decision, string Remarks);
public record AdvisoryCreateRequest(string Title, string District, string CropType, string Message, string OfficerName);
