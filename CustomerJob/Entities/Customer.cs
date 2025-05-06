

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerJob.Entities
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public int CustomerTypeId { get; set; }
        public string CustomerTypeName { get; set; } = null!;
        public int SegmentId { get; set; }
        public string CountryIso { get; set; } = null!;
        public string CurrencyIso { get; set; } = null!;
        public bool Deleted { get; set; }
        public DateTime LastChangeDateTime { get; set; }
        public ICollection<CustomerBrand> CustomerBrands { get; set; } = new List<CustomerBrand>();
    }
}
