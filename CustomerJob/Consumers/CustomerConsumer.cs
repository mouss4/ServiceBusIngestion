using Azure.Messaging.ServiceBus;
using CustomerJob.Contracts;
using CustomerJob.Models.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            _logger.LogInformation($"Message Received: {messageBody}"); 

            try
            {
                // Deserialize the message body into an object
                var messageObject = JsonSerializer.Deserialize<CustomerMessage>(messageBody);

                // Validate the deserialized object
                if (messageObject == null)
                {
                    _logger.LogWarning("Invalid message received. Skipping save.");
                    // Dead-letter the message
                    return;
                }

                // TODO: Send the message to the database

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
