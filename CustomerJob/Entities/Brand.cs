

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerJob.Entities
{
    public class Brand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BrandId { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<CustomerBrand> CustomerBrands { get; set; } = new List<CustomerBrand>();
    }
}
