using Azure.Messaging.ServiceBus;
using CustomerJob.Consumers;
using CustomerJob.Contracts;
using CustomerJob.Data;
using CustomerJob.Entities;
using CustomerJob.Models.Settings;
using CustomerJob.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Text.Json;

namespace CustomerJob.IntegrationTests
{
    public class CustomerConsumerTests : IAsyncLifetime
    {
        private readonly ServiceBusSettings? _serviceBusSettings;
        private readonly string _testConnectionString;
        private readonly int? retries = 10;

        private readonly ServiceBusClient _client;
        private ServiceBusSender _sender;

        private ServiceProvider _serviceProvider;
        private CustomerConsumer _consumer;

        public CustomerConsumerTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<CustomerConsumerTests>()
                .Build();

            _serviceBusSettings = configuration.GetSection("AzureServiceBus").Get<ServiceBusSettings>();

            if (_serviceBusSettings is null || 
                string.IsNullOrWhiteSpace(_serviceBusSettings.ConnectionString) ||
                string.IsNullOrWhiteSpace(_serviceBusSettings.QueueName))
            {
                throw new InvalidOperationException("Missing AzureServiceBus Test settings");
            }

            _testConnectionString = configuration.GetConnectionString("TestConnection");
            
            if (_testConnectionString is null || string.IsNullOrWhiteSpace(_testConnectionString))
                throw new InvalidOperationException("Missing TestConnectionString settings");
            
            _client = new ServiceBusClient(_serviceBusSettings.ConnectionString);

            _sender = _client.CreateSender(_serviceBusSettings.QueueName);
        }

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // InMemory EF Core setup
            services.AddDbContext<AppDbContext>(opt =>opt.UseNpgsql(_testConnectionString));

            services.AddScoped<IDataStorageService, DataStorageService>();

            services.Configure<ServiceBusSettings>(options =>
            {
                options.ConnectionString = _serviceBusSettings!.ConnectionString;
                options.QueueName = _serviceBusSettings.QueueName;
            });

            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();

            var logger = _serviceProvider.GetRequiredService<ILogger<CustomerConsumer>>();
            var options = _serviceProvider.GetRequiredService<IOptions<ServiceBusSettings>>();

            _consumer = new CustomerConsumer(options, logger, _serviceProvider);

            await _consumer.StartAsync(CancellationToken.None);
        }

        [Fact]
        public async Task Should_Store_SentMessage_In_TestDatabase()
        {
            // Arrange
            var testMessage = GetCustomerMessage();

            string jsonMessage = JsonSerializer.Serialize(testMessage);

            // Act
            await _sender.SendMessageAsync(new ServiceBusMessage(jsonMessage));

            await Task.Delay(3000); // Wait for the message to be processed

            // Assert
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Customer savedCustomer = null;

            for (int i = 0; i < retries; i++)
            {
                savedCustomer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == testMessage.CustomerId);
                if (savedCustomer != null)
                    break;

                await Task.Delay(300); // total max 3 seconds
            }

            Assert.NotNull(savedCustomer);
            Assert.Equal("Test Customer Sent", savedCustomer.Name);

        }
        public async Task DisposeAsync()
        {
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
            await _consumer.StopAsync(CancellationToken.None);

            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        private static CustomerMessage GetCustomerMessage()
        {
            return new CustomerMessage
            {
                CustomerId = 101,
                Name = "Test Customer Sent",
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