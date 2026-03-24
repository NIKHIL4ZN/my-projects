using Azure;
using Azure.Data.Tables;
using System;

namespace ARCRetailPOE.Models
{
    public class CustomerProfile 
    {
        public int Id { get; set; }              
        public string Name { get; set; }         
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }        
        public DateTime CreatedAt { get; set; }
    }
}

