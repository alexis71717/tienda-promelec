using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models
{
    public class Product
    {
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public required string Name { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Description { get; set; }

        [Required]
        [StringLength(100)]
        public required string Brand { get; set; }

        [Range(0.01, 9999999)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Url]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public long CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
