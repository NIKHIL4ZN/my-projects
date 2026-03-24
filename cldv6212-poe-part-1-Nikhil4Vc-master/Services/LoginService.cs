using ARCRetailPOE.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto.Generators;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCryptNet = BCrypt.Net.BCrypt;


namespace ARCRetailPOE.Services
{
    public class LoginService
    {
         private readonly string _connectionString;

        public LoginService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureSQLConnection");
        }

        public async Task<(bool success, string role)> AuthenticateUserAsync(string email, string password)
        {
            string query = "SELECT PasswordHash, Role FROM CustomerProfiles WHERE Email = @Email";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string storedHash = reader["PasswordHash"].ToString();
                        string role = reader["Role"].ToString();

                        bool valid = BCryptNet.Verify(password, storedHash);
                        return (valid, valid ? role : null);
                    }
                }
            }

            return (false, null);
        }
    }
}

