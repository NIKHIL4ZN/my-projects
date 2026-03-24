using Microsoft.AspNetCore.Identity;
using MonthlyClaims_System.Models;

namespace MonthlyClaims_System.Data
{
    public class SeedData
    {
        // Class used to handle user accounts
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Lecturer", "Coordinator", "Manager", "HR" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Holds users accounts in system (Other accounts found in database)
            var users = new List<(string Email, string Password, string Role)>
            {
                ("lecturer@claims.com", "Password123!", "Lecturer"),
                ("coordinator@claims.com", "Password123!", "Coordinator"),
                ("manager@claims.com", "Password123!", "Manager"),
                ("hr@claims.com", "Password123!",     "HR") 
            };

            foreach (var (email, password, role) in users)
            {
                var existingUser = await userManager.FindByEmailAsync(email);

                if (existingUser == null)
                {
                    var user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create user {email}: " +
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(existingUser, role))
                    {
                        await userManager.AddToRoleAsync(existingUser, role);
                    }
                }
            }

            // Method handles hourly rate field between Lecturer and HR users used for automated calculation
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();

                if (!context.ClaimRates.Any())
                {
                    context.ClaimRates.Add(new ClaimRate
                    {
                        HourlyRate = 250m 
                    });
                    await context.SaveChangesAsync();
                }
            }

        }
    }
}

