using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models
{
    public class Category
    {
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
