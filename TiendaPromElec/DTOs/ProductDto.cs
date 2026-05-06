using System.ComponentModel.DataAnnotations;

namespace TiendaPromElec.DTOs
{
    public class ProductCreateDto
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Range(0.01, 9999999)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Url]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public long CategoryId { get; set; }
    }

    public class ProductUpdateDto : ProductCreateDto { }
}
