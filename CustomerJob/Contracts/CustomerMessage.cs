using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Contracts
{
    public class CustomerMessage
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be positive")]
        public int CustomerId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int CustomerTypeId { get; set; }

        [Required]
        public string CustomerTypeName { get; set; } = null!;

        [Required]
        public int SegmentId { get; set; }

        [Required]
        public string CountryIso { get; set; } = null!;

        [Required]
        public string CurrencyIso { get; set; } = null!;

        public bool Deleted { get; set; }

        [Required]
        public List<BrandDto> Brands { get; set; } = new();

        public DateTime LastChangeDateTime { get; set; }
    }

    public class BrandDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BrandId { get; set; }

        [Required]
        public string Name { get; set; } = null!;
    }
}
