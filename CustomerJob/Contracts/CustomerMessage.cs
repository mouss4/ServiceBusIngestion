using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Contracts
{
    public class CustomerMessage
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public int CustomerTypeId { get; set; }
        public string CustomerTypeName { get; set; } = null!;
        public int SegmentId { get; set; }
        public string CountryIso { get; set; } = null!;
        public string CurrencyIso { get; set; } = null!;
        public bool Deleted { get; set; }
        public List<BrandDto> Brands { get; set; } = new();
        public DateTime LastChangeDateTime { get; set; }
    }

    public class BrandDto
    {
        public int BrandId { get; set; }
        public string Name { get; set; } = null!;
    }
}
