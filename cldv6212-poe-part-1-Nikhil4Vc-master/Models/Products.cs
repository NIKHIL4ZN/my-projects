using Azure;
using Azure.Data.Tables;
using System;
using System.Runtime.Serialization;

namespace ARCRetailPOE.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public double PriceValue { get; set; }

        [IgnoreDataMember] 
        public decimal Price
        {
            get => Convert.ToDecimal(PriceValue);
            set => PriceValue = Convert.ToDouble(value);
        }

        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


