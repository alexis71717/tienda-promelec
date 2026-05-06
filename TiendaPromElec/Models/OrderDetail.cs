using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models
{
    public class OrderDetail
    {
        public long Id { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public long ProductId { get; set; }
        public Product? Product { get; set; }

        public long OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
