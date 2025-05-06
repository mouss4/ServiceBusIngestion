using CustomerJob.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Services
{
    public interface IDataStorageService
    {
        /// <summary>
        /// Stores or updates a customer in the database based on the provided message.
        /// If the customer exists, it updates their details; if not, it creates a new customer.
        /// The method also manages customer-brand relationships and ensures brands are added or updated as needed.
        /// </summary>
        /// <param name="message">The customer message consumed from the ServiceBus.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StoreCustomerAsync(CustomerMessage customerMessage);
    }
}
