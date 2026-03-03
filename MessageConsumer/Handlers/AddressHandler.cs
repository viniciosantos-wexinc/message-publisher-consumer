using Messages.Models;
using Wex.Libraries.Kafka;
using Wex.Libraries.Kafka.Consumer;

namespace MessageConsumer.Handlers;

public class AddressHandler : IHandler<AddressChangeDetected>
{
    public async Task HandleAsync(Message<AddressChangeDetected> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received Address Change message: {message.Value}");
    }
}