using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models
{
    public class Order
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public required string Status { get; set; }

        public long CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public ICollection<OrderDetail>? Items { get; set; }
    }
}
