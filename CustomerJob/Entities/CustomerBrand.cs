using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Entities
{
    public class CustomerBrand
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int BrandId { get; set; }
        public Brand Brand { get; set; } = null!;
    }
}
