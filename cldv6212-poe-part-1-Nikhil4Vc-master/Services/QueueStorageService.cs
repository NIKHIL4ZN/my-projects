using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ARCRetailPOE.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ContainerConnection");
            var queueName = configuration["QueueName"] ?? "order-processing";

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));

            _queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            await _queueClient.SendMessageAsync(message);
        }

        public async Task<string> PeekNextMessageAsync()
        {
            var messages = await _queueClient.PeekMessagesAsync(1);
            return messages.Value.Length > 0 ? messages.Value[0].MessageText : "Queue is empty.";
        }
    }
}

