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
    // AddressChangeDetected
    await Publisher.PublishFromAsync(
        scope,
        messages.AddressChangeDetected.Topic,
        fixture
            .Create<AddressChangeDetected>());

    // SubscribedToReclaim
    // await Publisher.PublishFromAsync(scope, messages.SubscribedToReclaim.Topic, fixture.Create<SubscribedToReclaim>());

    // // UserChangeDetected
    // await Publisher.PublishFromAsync(
    //     scope,
    //     messages.UserChangeDetected.Topic,
    //     fixture
    //         .Build<UserChangeDetected>()
    //         .With(x => x.ChangeType, ChangeType.Update)
    //         .With(x => x.UserId, 9038231)
    //         .With(x => x.PlanTypeIDs, [2097152])
    //         .Create());

    // EmployeeCreated
    // await Publisher.PublishFromAsync(
    //     scope,
    //     messages.EmployeeCreated.Topic,
    //     fixture
    //         .Build<EmployeeCreated>()
    //         .With(x => x.user_id, 2089326)
    //         .With(x => x.client_id, 290)
    //         .With(x => x.origin, EmployeeCreatedType.census_import)
    //         .Create());

    // ElectionChangeDetected
    // await Publisher.PublishFromAsync(
    //     scope,
    //     messages.ElectionChanged.Topic,
    //     fixture
    //         .Build<ElectionChangeDetected>()
    //         .With(x => x.UserId, 9038231)
    //         .With(x => x.ParentUserId, 9038231)
    //         .With(x => x.BenefitElectionId, 206481444)
    //         .With(x => x.ChangeType, ChangeType.Update)
    //         .With(x => x.IsExpanded, false)
    //         .Create());


    Console.WriteLine("Produce new message? (y/n)");
    var key = Console.ReadKey();

    if (key.Key != ConsoleKey.Y)
    {
        break;
    }
}