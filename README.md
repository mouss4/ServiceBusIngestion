
<!-- ABOUT THE PROJECT -->
## About The Project

**ServiceBusIngestion** is a Worker Service that listens to messages from an Azure Service Bus queue and stores them in a PostgreSQL database.


<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these simple steps.

### Installation

1. Clone the repo
   ```sh
   git clone https://github.com/mouss4/ServiceBusIngestion.git
   ```
2. Set connections and queue name in `appsettings.json`
   ```JS
    {
        //Other Configs
        "ConnectionStrings": {
            "DefaultConnection": "Server=<URL>;User Id=<USERNAME>;Password=<PASSWORD>;Database=<DATABASE>",
        },
        "AzureServiceBus":{
            "ConnectionString": "<AzureServiceBus_ConnectionString>",
            "QueueName": "<QueueName>"
        }
    }
   ```

<!-- USAGE EXAMPLES -->
## Usage

Once the application is running and properly configured, the console will display output indicating that the service has started and is actively listening to the specified Azure Service Bus queue.

The job will:

- Continuously monitor the configured Azure Service Bus queue for new messages.
- Persist each message to the connected database.
- Log informational and error messages using Serilog.
