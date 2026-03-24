using ARCRetailPOE.Models;
using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ARCRetailPOE.Controllers
{
    [Authorize] // Require login for everyone
    public class CustomerController : Controller
    {
        private readonly IConfiguration _configuration;

        public CustomerController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var customers = new List<CustomerProfile>();
            string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Id, FullName, Email, Role, CreatedAt FROM CustomerProfiles ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new CustomerProfile
                            {
                                Id = (int)reader["Id"],
                                Name = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Role = reader["Role"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"❌ Failed to load users: {ex.Message}";
            }

            return View(customers);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill in all fields correctly.";
                return View(model);
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM CustomerProfiles WHERE Email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", model.Email);
                        int count = (int)await checkCmd.ExecuteScalarAsync();

                        if (count > 0)
                        {
                            ViewBag.ErrorMessage = "⚠️ A user with this email already exists.";
                            return View(model);
                        }
                    }

                    if (string.IsNullOrEmpty(model.PasswordHash))
                    {
                        ViewBag.ErrorMessage = "Password is required.";
                        return View(model);
                    }

                    string passwordHash = BCryptNet.HashPassword(model.PasswordHash);

                    string insertQuery = @"
                        INSERT INTO CustomerProfiles (FullName, Email, PasswordHash, Role, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, @Role, @CreatedAt)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", model.Name);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@Role", string.IsNullOrEmpty(model.Role) ? "Customer" : model.Role);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                TempData["Message"] = $"✅ Successfully created {model.Role} '{model.Name}'.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"❌ Error creating user: {ex.Message}";
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            CustomerProfile customer = null;
            string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Id, FullName, Email, Role, CreatedAt FROM CustomerProfiles WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customer = new CustomerProfile
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["FullName"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    CreatedAt = (DateTime)reader["CreatedAt"]
                                };
                            }
                        }
                    }
                }

                if (customer == null)
                    return NotFound();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"❌ Error loading user: {ex.Message}";
            }

            return View(customer);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            CustomerProfile customer = null;
            string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT Id, FullName, Email, Role, CreatedAt FROM CustomerProfiles WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            customer = new CustomerProfile
                            {
                                Id = (int)reader["Id"],
                                Name = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Role = reader["Role"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            };
                        }
                    }
                }
            }

            if (customer == null)
                return NotFound();

            return View(customer);
        }


        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "DELETE FROM CustomerProfiles WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                TempData["Message"] = "✅ User deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error deleting user: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

