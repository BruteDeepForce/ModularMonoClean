namespace Modules.Tables.Domain;

public class Table
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Hall { get; set; } = "main";
    public string Status { get; set; } = "available";
    public int MinCapacity { get; set; } = 1;
    public int MaxCapacity { get; set; } = 4;
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? MergedIntoTableId { get; set; }
    public Guid? CurrentOrderId { get; set; }
    public string? Note { get; set; }
    public string? QrToken { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
