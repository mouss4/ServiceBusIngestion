using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Models.Settings
{
    public class ServiceBusSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
    }
}
