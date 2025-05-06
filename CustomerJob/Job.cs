using CustomerJob.Consumers;

namespace CustomerJob
{
    public class Job : BackgroundService
    {
        private readonly ILogger<Job> _logger;
        private readonly CustomerConsumer _consumer;

        public Job(ILogger<Job> logger, CustomerConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job starting...");

            // Start the consumer to listen to the Service Bus
            await _consumer.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job stopping...");

            // Stop the message consumer
            await _consumer.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
                => Task.CompletedTask; // No long-running operations here, managed by Start/Stop.
    }
}
