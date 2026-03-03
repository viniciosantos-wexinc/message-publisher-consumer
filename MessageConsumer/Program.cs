using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wex.Libraries.Kafka.Configuration;
using Wex.Libraries.Kafka.Consumer;
using Wex.Libraries.Kafka.DependencyInjection;

namespace MessageConsumer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName.ToLower()}.json"), optional: true, reloadOnChange: true);

        var config = builder.Configuration;

        var messages = config.GetSection("Messages").Get<Messages.Settings.Messages>()!;

        builder.Services
            .AddKafka(WexDivision.Health, typeof(Program).Assembly)
            .AddConsumer(myBuilder =>
            {
                myBuilder
                    .AddTopic(messages.AddressChangeDetected.Topic)
                    .AddRecord<Messages.Models.AddressChangeDetected>("wex.health.be.benefits.address_changed", null, [new TimeSpan(0, 0, 30)]);
            })
            .AddConsumer(myBuilder =>
            {
                myBuilder
                    .AddTopic(messages.SubscribedToReclaim.Topic)
                    .AddRecord<Messages.Models.SubscribedToReclaim>("EmployeeDataStream.Models.Kafka.SubscribedToReclaim", null, [new TimeSpan(0, 0, 30)]);
            })
            .AddTransient<IHandler<Messages.Models.AddressChangeDetected>, Handlers.AddressHandler>()
            .AddTransient<IHandler<Messages.Models.SubscribedToReclaim>, Handlers.SubscribedHandler>();

        var app = builder.Build();
        using var scope = app.Services.CreateScope();

        app.Run();
    }
}