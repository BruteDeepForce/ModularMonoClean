using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Users.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "waiter";
        public bool IsActive { get; set; } = true;
        public string? PinHash { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int OrderCount { get; set; }
    }
}
