# Getting Started - Consumer

### 1. Create Message Models

Create C# classes that represent your Kafka messages. Use `DataContract` and `DataMember` attributes to map to the Avro schema fields:

```csharp
using System.Runtime.Serialization;

[DataContract(Name = "address_changed", Namespace = "wex.health.be.benefits")]
public record AddressChangeDetected
{
    [DataMember(Name = "user_id")]
    public long UserId { get; set; }

    [DataMember(Name = "address_id")]
    public long AddressID { get; set; }

    [DataMember(Name = "change_type_description")]
    public ChangeType ChangeType { get; set; }
}

public enum ChangeType
{
    Undefined = 0,
    Insert = 1,
    Update = 2,
    Delete = 3
}
```

**Important:** The `Name` in `DataContract` must match the `name` field in your Avro schema, and the `Namespace` must match the `namespace` field.

### 2. Configure Application Settings

Create an `appsettings.json` file with your Kafka configuration:

```json
{
  "Kafka": {
    "BootstrapServers": "wex-shared-aws-dev-kafka-ue1-wex-eventing-dev.g.aivencloud.com:13440",
    "SecurityProtocol": "Ssl",
    "SslCaPem": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----",
    "SslCertificatePem": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----",
    "SslKeyPem": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----",
    "SchemaRegistry": {
      "Url": "https://wex-shared-aws-dev-kafka-ue1-wex-eventing-dev.g.aivencloud.com:13443",
      "UserName": "your-user-name",
      "Password": "your-password"
    }
  },
  "Messages": {
    "AddressChangeDetected": {
      "Topic": "dv-mbe.user.address-changed",
      "SchemaFile": "mbe.user.address-changed.avsc",
      "SchemaSubject": "dv-mbe.user.address-changed.schema"
    }
  }
}
```

**Note:** For local development, you can use `"SecurityProtocol": "PLAINTEXT"` and `"BootstrapServers": "localhost:9092"` if running Kafka locally.

The `BootstrapServers` is found int the Aiven service page, in the `Connection information` section and Appache Kafka tab.

![Bootstrap URL](../docs/images/bootstrap-url.png)

The `SchemaRegistry.Url` is found int the Aiven service page, in the `Connection information` section and Schema Registry tab.

![Schema Registry URL](../docs/images/registry-url.png)

## Usage

### 1. Create Message Handlers

Create handler classes that implement `IHandler<T>` to process incoming messages:

```csharp
using Messages.Models;
using Wex.Libraries.Kafka;
using Wex.Libraries.Kafka.Consumer;

namespace MessageConsumer.Handlers;

public class AddressHandler : IHandler<AddressChangeDetected>
{
    public async Task HandleAsync(Message<AddressChangeDetected> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received Address Change message: {message.Value}");

        // Add your business logic here
        // For example:
        // - Update database
        // - Call external APIs
        // - Trigger other processes
    }
}
```

**Important:** Each message type should have its own handler implementation.

### 2. Register Kafka Services

In your `Program.cs`, configure the Kafka consumer services using dependency injection:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wex.Libraries.Kafka.Configuration;
using Wex.Libraries.Kafka.Consumer;
using Wex.Libraries.Kafka.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName.ToLower()}.json",
                 optional: true, reloadOnChange: true);

// Register Kafka consumers
builder.Services
    .AddKafka(WexDivision.Health, typeof(Program).Assembly)
    .AddConsumer(myBuilder =>
    {
        myBuilder
            .AddTopic("dv-mbe.user.address-changed")  // Topic name
            .AddRecord<AddressChangeDetected>(
                "wex.health.be.benefits.address_changed",  // Schema name (namespace.name)
                null,  // Optional partition key
                [new TimeSpan(0, 0, 30)]  // Retry intervals
            );
    })
    .AddTransient<IHandler<AddressChangeDetected>, Handlers.AddressHandler>();

var app = builder.Build();
await app.RunAsync();
```

### 3. Multiple Consumers

You can register multiple consumers for different topics:

```csharp
builder.Services
    .AddKafka(WexDivision.Health, typeof(Program).Assembly)
    .AddConsumer(myBuilder =>
    {
        myBuilder
            .AddTopic("dv-mbe.user.address-changed")
            .AddRecord<AddressChangeDetected>("wex.health.be.benefits.address_changed", null, [new TimeSpan(0, 0, 30)]);
    })
    .AddConsumer(myBuilder =>
    {
        myBuilder
            .AddTopic("dv-event.mbe.employees")
            .AddRecord<SubscribedToReclaim>("EmployeeDataStream.Models.Kafka.SubscribedToReclaim", null, [new TimeSpan(0, 0, 30)]);
    })
    .AddTransient<IHandler<AddressChangeDetected>, Handlers.AddressHandler>()
    .AddTransient<IHandler<SubscribedToReclaim>, Handlers.SubscribedHandler>();
```

### 4. Understanding Retry Intervals

The retry intervals parameter `[new TimeSpan(0, 0, 30)]` defines how long to wait before retrying a failed message:

- First retry: 30 seconds after failure
- You can add multiple intervals: `[new TimeSpan(0, 0, 30), new TimeSpan(0, 1, 0), new TimeSpan(0, 5, 0)]`
  - First retry: 30 seconds
  - Second retry: 1 minute
  - Third retry: 5 minutes

## Troubleshooting

### Common Issues

1. **Schema not found error**
   - Ensure the schema name matches the format: `{namespace}.{name}` from your Avro schema
   - Verify the schema is registered in the Schema Registry
   - Check that the user has permissions for the schema subject

2. **Authentication failed**
   - Verify the SSL certificates are correctly formatted with `\r\n` line breaks
   - Ensure the Schema Registry username and password are correct
   - Check that the user has permissions for the topic and schema subject

3. **Topic not found**
   - Verify the topic exists in Aiven
   - Check that the topic name matches the environment prefix (e.g., `dv-`, `qa-`, `uat-`)
   - Ensure the user has read permissions for the topic

4. **Schema mismatch**
   - Verify the C# model's `DataContract` name and namespace match the Avro schema
   - Check that field names in `DataMember` attributes match the Avro schema fields
   - Ensure enum values match between C# and Avro schema

5. **Messages not being consumed**
   - Check that the consumer group ID is unique or appropriate for your use case
   - Verify the handler is registered in dependency injection
   - Ensure the topic has messages (check in Aiven console)
   - Check application logs for any errors

6. **Handler not executing**
   - Verify the handler implements `IHandler<T>` correctly
   - Ensure the handler is registered with `AddTransient<IHandler<T>, YourHandler>()`
   - Check that the message type `T` matches the type in `AddRecord<T>()`

### Testing Locally

For local development without connecting to Aiven:

1. Run Kafka locally using Docker:
   ```bash
   docker-compose up -d
   ```

2. Use `appsettings.debug.json` with:
   ```json
   {
     "Kafka": {
       "BootstrapServers": "localhost:9092",
       "SecurityProtocol": "PLAINTEXT"
     },
     "SchemaRegistry": {
        "Url": "https://wex-shared-aws-dev-kafka-ue1-wex-eventing-dev.g.aivencloud.com:13443",
        "Password": "...",
        "UserName": "..."
    }
   }
   ```

3. Run the powershell script `.\consumer.ps1` to start the consumer.

