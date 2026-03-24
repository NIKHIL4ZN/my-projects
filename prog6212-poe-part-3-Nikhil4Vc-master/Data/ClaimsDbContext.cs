using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonthlyClaims_System.Models;
using System.Collections.Generic;

namespace MonthlyClaims_System.Data
{
    public class ClaimsDbContext : IdentityDbContext
    {
        // Data class that handles changes to claim rates between the HR and Lecturer user that will be used in automated calculation
        public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options) { }

        public DbSet<Claim> Claims { get; set; }

        public DbSet<ClaimRate> ClaimRates { get; set; }

    }
}
