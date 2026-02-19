using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Identity.DTO
{
    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
    }
}