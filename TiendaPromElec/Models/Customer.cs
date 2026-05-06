using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models
{
    public class Customer
    {
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public required string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public required string Phone { get; set; }

        [Required]
        [StringLength(500)]
        public required string Address { get; set; }

        public ICollection<Order>? Orders { get; set; }
    }
}
