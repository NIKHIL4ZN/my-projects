using System.ComponentModel.DataAnnotations;

namespace ARCRetailPOE.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }   
        public decimal Total { get; set; }   
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}

