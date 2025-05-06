using CustomerJob.Contracts;
using CustomerJob.Data;
using CustomerJob.Entities;
using CustomerJob.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql.Replication.PgOutput.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.UnitTests
{
    public class DataStorageServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<DataStorageService>> _mockLogger;
        private readonly DataStorageService _dataService;

        public DataStorageServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<DataStorageService>>();
            _dataService = new DataStorageService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task StoreCustomerAsync_ShouldCreateNewCustomer_WhenCustomerDoesNotExist()
        {
            // Arrange
            var testMessage = GetCustomerMessage();

            // Act
            await _dataService.StoreCustomerAsync(testMessage);

            // Assert
            var customer = await _context.Customers
                .Include(c => c.CustomerBrands)
                .FirstOrDefaultAsync(c => c.CustomerId == 101);

            Assert.NotNull(customer);
            Assert.Equal("Test Customer - New", customer.Name);
            Assert.Equal(2, customer.CustomerTypeId);
            Assert.False(customer.Deleted);
            Assert.Single(customer.CustomerBrands); // 1 brand linked

            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandId == 1);
            Assert.NotNull(brand);
            Assert.Equal("NewBrand", brand.Name);
        }

        [Fact]
        public async Task StoreCustomerAsync_ShouldUpdateCustomer_WhenCustomerExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isolate this test
                .Options;

            using var tempContext = new AppDbContext(options);
            // Seed database with existing customer and brand

            var existingCustomer = new Customer
            {
                CustomerId = 101,
                Name = "Existing Customer",
                CustomerTypeId = 1,
                CustomerTypeName = "TestAccount",
                SegmentId = 10,
                CountryIso = "SWE",
                CurrencyIso = "SEK",
                Deleted = false,
                LastChangeDateTime = DateTime.UtcNow,
                CustomerBrands = new List<CustomerBrand>
                {
                    new CustomerBrand { BrandId = 1, CustomerId = 101 }
                }
            };

            var existingbrand = new Brand { BrandId = 1, Name = "Old Brand" };

            tempContext.Customers.Add(existingCustomer);
            tempContext.Brands.Add(existingbrand);
            await tempContext.SaveChangesAsync();

            var newCustomerMessage = GetCustomerMessage();

            // Act
            var service = new DataStorageService(tempContext, _mockLogger.Object);

            await service.StoreCustomerAsync(newCustomerMessage);

            // Assert
            var customer = await tempContext.Customers
                .Include(c => c.CustomerBrands)
                .FirstOrDefaultAsync(c => c.CustomerId == 101);

            Assert.NotNull(customer);
            Assert.Equal("Test Customer - New", customer.Name);
            Assert.Equal(2, customer.CustomerTypeId);
            Assert.False(customer.Deleted);
            Assert.Single(customer.CustomerBrands);
            Assert.Equal(1, customer.CustomerBrands.First().BrandId);

            var brand = await tempContext.Brands.FirstOrDefaultAsync(b => b.BrandId == 1);
            Assert.NotNull(brand);
            //Assert.Equal("NewBrand", brand.Name);

        }
        
        private static CustomerMessage GetCustomerMessage()
        {
            return new CustomerMessage
            {
                CustomerId = 101,
                Name = "Test Customer - New",
                CustomerTypeId = 2,
                CustomerTypeName = "TestAccount",
                SegmentId = 10,
                CountryIso = "SWE",
                CurrencyIso = "SEK",
                Deleted = false,
                LastChangeDateTime = DateTime.UtcNow,
                Brands = new List<BrandDto> {
                    new BrandDto
                    {
                        BrandId = 1,
                        Name = "NewBrand"
                    }
                }
            };
        }
    }
}
