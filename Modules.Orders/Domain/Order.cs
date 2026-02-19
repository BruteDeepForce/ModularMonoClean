using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Orders.Domain
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TableId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string OrderType { get; set; } = "table";
        public string Status { get; set; } = "pending";
        public string Priority { get; set; } = "normal";
        public string Source { get; set; } = "pos";
        public DateTime? ScheduledAtUtc { get; set; }
        public string? Note { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
