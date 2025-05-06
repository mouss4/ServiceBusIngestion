using Azure.Messaging.ServiceBus;
using CustomerJob.Contracts;
using CustomerJob.Models.Settings;
using CustomerJob.Services;
using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;

using System.Text.Json;

namespace CustomerJob.Consumers
{
    public class CustomerConsumer
    {
        private readonly ServiceBusSettings _settings;
        private readonly ILogger<CustomerConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        private ServiceBusClient? _client;
        private ServiceBusProcessor? _processor;

        public CustomerConsumer(
            IOptions<ServiceBusSettings> options,
            ILogger<CustomerConsumer> logger,
            IServiceProvider serviceProvider)
        {
            _settings = options.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _client = new ServiceBusClient(_settings.ConnectionString);
            _processor = _client.CreateProcessor(_settings.QueueName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += MessageHandlerAsync;
            _processor.ProcessErrorAsync += ErrorHandlerAsync;

            await _processor.StartProcessingAsync(cancellationToken);
            _logger.LogInformation("Started listening to queue: {Queue}", _settings.QueueName);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor is not null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                _logger.LogInformation("Stoped listening to queue: {Queue}", _settings.QueueName);
            }
        }

        private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
        {
            var messageBody = args.Message.Body.ToString();
            _logger.LogInformation("Message Received! Processing...");
            //_logger.LogInformation($"Message Received: {messageBody}"); 

            try
            {
                // Deserialize the message body into an object
                var messageObject = JsonSerializer.Deserialize<CustomerMessage>(messageBody) ?? new();

                // Validate deserialized CustomerMessage using data annotations
                var validationContext = new ValidationContext(messageObject);
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(messageObject, validationContext, validationResults, true);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid message received. Skipping save.");

                    await args.DeadLetterMessageAsync(args.Message);

                    _logger.LogInformation($"Message moved to Dead Letter Queue");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var storageService = scope.ServiceProvider.GetRequiredService<IDataStorageService>();

                // Save the received message to the database
                await storageService.StoreCustomerAsync(messageObject);

                await args.CompleteMessageAsync(args.Message);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing message");
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", jsonEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message.");
                throw;
            }
        }

        private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Srvice Bus error occurred");
            return Task.CompletedTask;
        }

    }
}
