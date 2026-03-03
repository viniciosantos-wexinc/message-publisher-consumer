using Messages.Models;
using Wex.Libraries.Kafka;
using Wex.Libraries.Kafka.Consumer;

namespace MessageConsumer.Handlers;

public class SubscribedHandler : IHandler<SubscribedToReclaim>
{
    public async Task HandleAsync(Message<SubscribedToReclaim> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received Subscribed message: {message.Value}");
    }
}