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
- [Getting Started - Publisher](MessagePublisher/Readme.md)
- [Getting Started - Consumer](MessageConsumer/Readme.md)

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

