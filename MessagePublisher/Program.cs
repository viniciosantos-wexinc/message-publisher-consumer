using AutoFixture;
using MessagePublisher;
using Messages.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wex.Libraries.Kafka.Configuration;
using Wex.Libraries.Kafka.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName.ToLower()}.json"), optional: true, reloadOnChange: true);

var config = builder.Configuration;

var messages = config.GetSection("Messages").Get<Messages.Settings.Messages>()!;

builder.Services
    .AddKafka(WexDivision.Health, typeof(Program).Assembly)
    .AddProducer<AddressChangeDetected>(
        messages.AddressChangeDetected.SchemaSubject,
        messages.AddressChangeDetected.GetSchemaPath())
    .AddProducer<SubscribedToReclaim>(
        messages.SubscribedToReclaim.SchemaSubject,
        messages.SubscribedToReclaim.GetSchemaPath())
    .AddProducer<UserChangeDetected>(
        messages.UserChangeDetected.SchemaSubject,
        messages.UserChangeDetected.GetSchemaPath())
    .AddProducer<EmployeeCreated>(
        messages.EmployeeCreated.SchemaSubject,
        messages.EmployeeCreated.GetSchemaPath())
    .AddProducer<ElectionChangeDetected>(
        messages.ElectionChanged.SchemaSubject,
        messages.ElectionChanged.GetSchemaPath());

var app = builder.Build();
var fixture = new Fixture();
using var scope = app.Services.CreateScope();

while (true)
{

    if (DateTime.Now.Second % 2 == 0)
    {
        // AddressChangeDetected
        await Publisher.PublishFromAsync(scope, messages.AddressChangeDetected.Topic, fixture.Create<AddressChangeDetected>());
    }
    else
    {
        // SubscribedToReclaim
        await Publisher.PublishFromAsync(scope, messages.SubscribedToReclaim.Topic, fixture.Create<SubscribedToReclaim>());
    }

    Console.WriteLine("Produce new message? (y/n)");
    var key = Console.ReadKey();

    if (key.Key != ConsoleKey.Y)
    {
        break;
    }
}