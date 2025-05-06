using CustomerJob.Contracts;
using CustomerJob.Data;
using CustomerJob.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerJob.Services
{
    public class DataStorageService : IDataStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataStorageService> _logger;

        public DataStorageService(AppDbContext context, ILogger<DataStorageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task StoreCustomerAsync(CustomerMessage message)
        {
            _logger.LogInformation("Storing customer {CustomerId}", message.CustomerId);
            var customer = await _context.Customers
                    .Include(x => x.CustomerBrands)
                    .FirstOrDefaultAsync(c => c.CustomerId == message.CustomerId);

            if (customer is null)
            {
                _logger.LogInformation("Creating new customer");

                customer = new Customer
                {
                    CustomerId = message.CustomerId,
                    Name = message.Name,
                    CustomerTypeId = message.CustomerTypeId,
                    CustomerTypeName = message.CustomerTypeName,
                    SegmentId = message.SegmentId,
                    CountryIso = message.CountryIso,
                    CurrencyIso = message.CurrencyIso,
                    Deleted = message.Deleted,
                    LastChangeDateTime = message.LastChangeDateTime.ToUniversalTime(),
                    CustomerBrands = new List<CustomerBrand>()
                };

                _context.Customers.Add(customer);
            }
            else
            {
                _logger.LogInformation("Updating existing customer");

                customer.Name = message.Name;
                customer.CustomerTypeId = message.CustomerTypeId;
                customer.CustomerTypeName = message.CustomerTypeName;
                customer.SegmentId = message.SegmentId;
                customer.CountryIso = message.CountryIso;
                customer.CurrencyIso = message.CurrencyIso;
                customer.Deleted = message.Deleted;
                customer.LastChangeDateTime = message.LastChangeDateTime.ToUniversalTime();

                // Remove existing customer-brand links
                customer.CustomerBrands.Clear();
            }

            // Ensure all brands exist
            foreach (var brand in message.Brands)
            {
                var brandEntity = await _context.Brands.FindAsync(brand.BrandId);
                if (brandEntity is null)
                {
                    brandEntity = new Brand
                    {
                        BrandId = brand.BrandId,
                        Name = brand.Name
                    };

                    _context.Brands.Add(brandEntity);
                }
            }

            // Save all new brands before linking them
            await _context.SaveChangesAsync();

            // Add CustomerBrand relations
            foreach (var brand in message.Brands)
            {
                customer.CustomerBrands.Add(new CustomerBrand
                {
                    CustomerId = customer.CustomerId,
                    BrandId = brand.BrandId
                });
            }

            // Save all changes including CustomerBrand
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer saved successfully");
        }
    }
}
