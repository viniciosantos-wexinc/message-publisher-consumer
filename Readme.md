# Overview
This project is a demo of how to publish and consume messages to and from Kafka using the `Wex.Libraries.Kafka` library.

Prerequisites:
- Topics, schemas and users created in Aiven
- Kafka library `Wex.Libraries.Kafka` (version 8.4.0 or higher)
- .NET 8.0 SDK

## Table of Contents
- [Aiven](#aiven)
  - [Projects](#projects)
  - [Services](#services)
  - [Topics](#topics)
  - [User](#user)
- [Getting Started](#getting-started)
  - [1. Create Message Models](#1-create-message-models)
  - [2. Store Avro Schemas Locally](#2-store-avro-schemas-locally)
  - [3. Configure Application Settings](#3-configure-application-settings)
- [Usage](#usage)
  - [1. Register Kafka Services](#1-register-kafka-services)
  - [2. Publish Messages](#2-publish-messages)
  - [3. Helper Method (Optional)](#3-helper-method-optional)
- [Troubleshooting](#troubleshooting)
  - [Common Issues](#common-issues)
  - [Testing Locally](#testing-locally)

# Aiven
We host the schema, topics and users in Aiven. It is accessed through the `Aiven` tile in `myapps`, then selecting the project and then the service.

## Projects
Specific for environments we have:
- `wex-eventing-dev` for dev and qa
- `wex-eventing-stage` for uat
- `wex-eventing-prod` for prod

## Services
We use the service `wex-shared-aws-{env}-kafka-ue1` where `{env}` corresponds to the environment (`dev`, `stage` or `prod`).

### Topics
The topics are created through the [kaas-eventing-resources](https://github.com/wex-gts/kaas-eventing-resources) repository, adding the schema (_.avsc_ file) and the topic configuration (_.yaml_ file) to the corresponding team  and environment folders.

#### Schema
We created the schemas following the [Avro schema](https://avro.apache.org/docs/1.11.1/specification/) specification. The idea is to add the fields we want to send in the message.

Below is an example of a schema for the `ElectionChangeDetected` message.
The `name` and `namespace` can be any string the team prefer. For the `namespace` is recommended to use something related to the domain or business logic. Think of namespace as a way to group related schemas, similar to a package.

Note that in the fields we have a variation of types, like `int`, `boolean` and `enum`. The `enum` is a way to define a set of possible values for a field. For `nullable` values we use an array with the first element as `null` and the second element as the type.

```json
{
  "type": "record",
  "name": "election_changed",
  "namespace": "wex.health.be.benefits",
  "fields": [
    {
      "name": "user_id",
      "type": "int"
    },
    {
      "name": "parent_user_id",
      "type": [ "null", "int" ],
      "default": null
    },
    {
      "name": "benefit_election_id",
      "type": "int"
    },
    {
      "name": "is_expanded",
      "type": "boolean"
    },
    {
      "name": "change_type_description",
      "type": {
        "type": "enum",
        "name": "ChangeType",
        "symbols": [ "undefined", "insert", "update", "delete" ]
      }
    }
  ]
}
```
<small>Schema example found in https://github.com/wex-gts/kaas-eventing-resources/blob/main/teams/BEBenefits-v2/stage/mbe.employee.election-changed.avsc</small>

#### Topic
When creating the topic, we used the following template for a simple configuration.

In the schemas we need to reference the name of the _.avsc_ `file` we created and define the `subject` that will be used to register the schema in the schema registry.

```yaml
- kind: topic
  description: Application
  name: uat-mbe.employee.election-changed
  termination_protection: false
  config:
    min_insync_replicas: 2
    retention_bytes: 1073741824
    retention_ms: 604800000
  schemas:
    - file: mbe.employee.election-changed.avsc
      subject: uat-mbe.employee.election-changed.schema
```
<small>Topic example found in https://github.com/wex-gts/kaas-eventing-resources/blob/main/teams/BEBenefits-v2/stage/mbe.employee.election-changed.yaml</small>

### User
We need a user to be able to publish and consume messages from the topics. The user is created adding the configuration (_.yaml_ file) to the corresponding team and environment folders.

In the `use` section we need to add the topics name we want to use. For the `schemas` we need to add the subjects we want to use. And finally in the `readwrite` section we need to add the user name we're creating.

```yaml
- kind: user
  use:
    - uat-event.mbe.employees
    [...]
    - uat-mbe.user.rate-discriminator-changed
  schemas:
    - subject: uat-event.mbe.employees.employeeCreated.schema
    [...]
    - subject: wex.enterprise.key
  readwrite:
    - uat-event.mbe.employee-data-stream-universal-user
```
<small>User example found in https://github.com/wex-gts/kaas-eventing-resources/blob/main/teams/cloud-9-v2/stage/event.mbe.employee-data-stream-universal-user.yaml</small>

# Getting Started

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

### 2. Store Avro Schemas Locally

**\[Suggestion]**: Create a `schemas` folder in your project and add your `.avsc` files. Make sure they are copied to the output directory by adding this to your `.csproj`:

```xml
<ItemGroup>
  <None Update="schemas/*.avsc">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### 3. Configure Application Settings

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

![Bootstrap URL](docs/images/bootstrap-url.png)

The `SchemaRegistry.Url` is found int the Aiven service page, in the `Connection information` section and Schema Registry tab.

![Schema Registry URL](docs/images/registry-url.png)
## Usage

### 1. Register Kafka Services

In your `Program.cs`, configure the Kafka services using dependency injection:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wex.Libraries.Kafka.Configuration;
using Wex.Libraries.Kafka.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName.ToLower()}.json",
                 optional: true, reloadOnChange: true);

// Register Kafka producers
builder.Services
    .AddKafka(WexDivision.Health, typeof(Program).Assembly)
    .AddProducer<AddressChangeDetected>(
        "dv-mbe.user.address-changed.schema",  // Schema subject
        "schemas/mbe.user.address-changed.avsc" // Schema file path
    );

var app = builder.Build();
```

### 2. Publish Messages

Use the `IKafkaProducer<T>` service to publish messages:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Wex.Libraries.Kafka.Producer;

using var scope = app.Services.CreateScope();
var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer<AddressChangeDetected>>();

var message = new AddressChangeDetected
{
    UserId = 9018121,
    AddressID = 4389775,
    ChangeType = ChangeType.Update
};

try
{
    await producer.ProduceAsync(
        topic: "dv-mbe.user.address-changed",
        key: Guid.NewGuid().ToString(),
        message: new Message<AddressChangeDetected>(
            Guid.NewGuid().ToString(), // Idempotency key
            message
        ),
        cancellationToken: CancellationToken.None
    );

    Console.WriteLine($"Message published successfully: {message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to publish message: {ex.Message}");
}
```

### 3. Helper Method (Optional)

You can create a helper method to simplify publishing:

```csharp
public static class Publisher
{
    public static async Task PublishFromAsync<T>(
        IServiceScope scope,
        string topic,
        T message,
        string? idempotencyKey = null)
    {
        var service = scope.ServiceProvider.GetRequiredService<IKafkaProducer<T>>();

        try
        {
            await service.ProduceAsync(
                topic,
                Guid.NewGuid().ToString(),
                new Message<T>(idempotencyKey ?? Guid.NewGuid().ToString(), message),
                CancellationToken.None
            );

            Console.WriteLine($"Message produced: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to produce message: {ex.Message}");
        }
    }
}

// Usage
await Publisher.PublishFromAsync(scope, "dv-mbe.user.address-changed", message);
```
## Troubleshooting

### Common Issues

1. **Schema not found error**
   - Ensure the `.avsc` file exists in the `schemas` folder
   - Verify the file is being copied to the output directory
   - Check that the schema file name in configuration matches the actual file name

2. **Authentication failed**
   - Verify the SSL certificates are correctly formatted with `\r\n` line breaks
   - Ensure the Schema Registry username and password are correct
   - Check that the user has permissions for the topic and schema subject

3. **Topic not found**
   - Verify the topic exists in Aiven
   - Check that the topic name matches the environment prefix (e.g., `dv-`, `qa-`, `uat-`)
   - Ensure the user has read/write permissions for the topic

4. **Schema mismatch**
   - Verify the C# model's `DataContract` name and namespace match the Avro schema
   - Check that field names in `DataMember` attributes match the Avro schema fields
   - Ensure enum values match between C# and Avro schema

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

3. Set the environment variable:
   ```bash
   $env:DOTNET_ENVIRONMENT="Debug"
   ```
