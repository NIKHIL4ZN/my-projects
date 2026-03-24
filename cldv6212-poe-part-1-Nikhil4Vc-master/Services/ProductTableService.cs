using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARCRetailPOE.Models;

namespace ARCRetailPOE.Services
{
    public class ProductTableService
    {
        private readonly TableClient _tableClient;

        public ProductTableService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ContainerConnection");
            var tableName = configuration["ProductTableName"] ?? "products";

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            _tableClient = new TableClient(connectionString, tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _tableClient.QueryAsync<Product>("PartitionKey eq 'Product'"))
            {
                products.Add(product);
            }
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            if (product.CreatedAt.Kind != DateTimeKind.Utc)
            {
                product.CreatedAt = DateTime.SpecifyKind(product.CreatedAt, DateTimeKind.Utc);
            }
            await _tableClient.AddEntityAsync(product);
        }

        public async Task<Product> GetProductAsync(string rowKey)
        {
            try
            {
                return await _tableClient.GetEntityAsync<Product>("Product", rowKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            if (product.CreatedAt.Kind != DateTimeKind.Utc)
            {
                product.CreatedAt = DateTime.SpecifyKind(product.CreatedAt, DateTimeKind.Utc);
            }
            await _tableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string rowKey)
        {
            await _tableClient.DeleteEntityAsync("Product", rowKey);
        }
    }
}

