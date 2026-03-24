using ARCRetailPOE.Models;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ARCRetailPOE.Services
{
    public class OrderService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public OrderService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQLConnection");
        }

        public async Task AddOrderAsync(Order order)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO Orders (CustomerEmail, ProductName, Quantity, Price, Total, Status, OrderDate)
                    VALUES (@CustomerEmail, @ProductName, @Quantity, @Price, @Total, @Status, @OrderDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerEmail", order.CustomerEmail);
                    cmd.Parameters.AddWithValue("@ProductName", order.ProductName);
                    cmd.Parameters.AddWithValue("@Quantity", order.Quantity);
                    cmd.Parameters.AddWithValue("@Price", order.Price);
                    cmd.Parameters.AddWithValue("@Total", order.Total);
                    cmd.Parameters.AddWithValue("@Status", order.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("@OrderDate", order.OrderDate);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Orders ORDER BY OrderDate DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new Order
                            {
                                OrderId = (int)reader["OrderId"],
                                CustomerEmail = reader["CustomerEmail"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                Price = (decimal)reader["Price"],
                                Total = (decimal)reader["Total"],
                                Status = reader["Status"].ToString(),
                                OrderDate = (DateTime)reader["OrderDate"]
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string email)
        {
            var orders = new List<Order>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Orders WHERE CustomerEmail = @Email ORDER BY OrderDate DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new Order
                            {
                                OrderId = (int)reader["OrderId"],
                                CustomerEmail = reader["CustomerEmail"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                Price = (decimal)reader["Price"],
                                Total = (decimal)reader["Total"],
                                Status = reader["Status"].ToString(),
                                OrderDate = (DateTime)reader["OrderDate"]
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}

