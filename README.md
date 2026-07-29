# Flow OverStack – NotificationService

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=coverage)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)

## Project Overview

NotificationService is a microservice in the Flow OverStack platform responsible for
turning domain events raised by [UserService](https://github.com/flow-OverStack/UserService),
[QuestionService](https://github.com/flow-OverStack/QuestionService), and
[AnswerService](https://github.com/flow-OverStack/AnswerService) into user-facing
notifications. It consumes events from Kafka, persists notification history, and pushes
new notifications to connected clients in real time over SignalR.

## 🚀 Quick Start a ready-made API

1. Install [Docker Desktop](https://www.docker.com/)
2. [Quick Start](https://github.com/flow-OverStack/UserService?tab=readme-ov-file#-quick-start-a-ready-made-api) the User Service (and, optionally, Question/Answer Service) so there are events to notify about.
3. Copy [the docker-compose.yaml](https://github.com/flow-OverStack/NotificationService/blob/master/docker-compose.yaml) file into one directory
4. Copy (and reconfigure if needed) [logstash.conf](https://github.com/flow-OverStack/NotificationService/blob/master/logstash.conf) file in the same directory
5. Create and configure `.env` file in the same directory:
   ```env
   NOTIFICATION_DB_PASSWORD=db_password
   REDIS_PASSWORD=redis_password
   ```
6. On the first run (or after updating migrations), apply EF Core migrations to the database:

   **Option A — Automatic ✅ Recommended for Quick Start**

   In `docker-compose.yaml`, temporarily add `ASPNETCORE_ENVIRONMENT: Development` to the `notification-service` environment:
   ```yaml
   notification-service:
      # ... other variables
      environment:
        # ... other variables
        ASPNETCORE_ENVIRONMENT: Development
   ```
   Start the services — migrations will be applied automatically on startup.
   > ⚠️ After the first run, **remove** `ASPNETCORE_ENVIRONMENT: Development` from `docker-compose.yaml` and restart the container.

   **Option B — Manual SQL script (Production)**

   Generate a SQL script with `dotnet ef migrations script` and apply it to the database
      manually ([Production approach](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli#sql-scripts))
7. Start the service
    ```bash
   docker-compose -p notificationservice -f docker-compose.yaml up -d
   ```
8. Explore endpoints at `/swagger/v1/swagger.json` endpoint.

## Technologies and Patterns Used

* **.NET 10 & C#** — Core framework and language
* **ASP.NET Core** — HTTP API
* **SignalR** — Real-time push of notifications, with a Redis backplane for multi-instance fan-out
* **Entity Framework Core with PostgreSQL** — Data access to PostgreSQL database
* **Kafka (MassTransit)** — Message queue that listens to events from UserService/QuestionService/AnswerService
* **Redis** — Caching layer with short-lived entity caching, used via the Decorator pattern
* **Clean Architecture** — Layered separation (Domain, Application, Infrastructure, Presentation)
* **Decorator Pattern** — allows behavior to be added to individual objects dynamically without affecting others. In this project, it is used to implement caching.
* **Keycloak** — OAuth2/OpenID Connect identity provider for JWT validation
* **Hangfire** — Scheduled redelivery of failed Kafka messages, backed by PostgreSQL
* **Resilience** — Retry-then-dead-letter consume filter, plus a MassTransit kill switch, for Kafka message processing
* **Observability** — Traces, logs, and metrics collected via OpenTelemetry and Logstash, exported to Aspire dashboard, Jaeger, ElasticSearch, and Prometheus
* **Monitoring & Visualization** — Dashboards in Grafana, Kibana, and Aspire
* **Health Checks** — Status endpoints to monitor service availability and dependencies
* **xUnit** — Automated unit and functional testing (Moq/MockQueryable for units, Testcontainers/WireMock for functional)
* **SonarQube** — Code quality and coverage analysis

## Architecture and Design

This service follows the principles of Clean Architecture. The solution is split into
multiple projects that correspond to each architectural layer.

![Clean Architecture](https://www.milanjovanovic.tech/blogs/mnw_017/clean_architecture.png?imwidth=1920)

| Layer              | Project                                                                                |
|--------------------|-----------------------------------------------------------------------------------------|
| **Presentation**   | NotificationService.Api                                                                 |
| **Application**    | NotificationService.Application                                                         |
| **Domain**         | NotificationService.Domain                                                              |
| **Infrastructure** | NotificationService.Cache, NotificationService.DAL, NotificationService.Messaging       |

Unlike its siblings, NotificationService does not expose GraphQL or gRPC endpoints — it is
REST + SignalR only, and it does not use the Outbox pattern since it is a pure event
consumer, not a producer.

## Getting Started for developers

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Docker Desktop](https://www.docker.com/)

### Installation

1. Clone the repo
2. Start dependencies (you can use [Quick Start](#-quick-start-a-ready-made-api) without running the `notification-service` container, or run your own services)
3. Reconfigure if needed `appsettings.json` and `.NET User Secrets` in `NotificationService.Api` with your database, Redis, and Keycloak settings.
   `.NET User Secrets` example:
   ```json
   {
      "ConnectionStrings": {
         "PostgresSQL": "Server=localhost;Port=5437; Database=notification-service-db; User Id=<YOUR-USER-ID>; Password=<YOUR-PASSWORD>"
      },
      "RedisSettings": {
        "Password": "<YOUR-PASSWORD>"
      }
   }
   ```
4. Run the API:

   ```bash
   cd NotificationService.Api
   dotnet run
   ```
   or use your IDE.

## API Documentation

REST API & Swagger are available at `http://localhost:5214/swagger/v1/swagger.json`
(`https://localhost:7233` also available in the `https` launch profile). The SignalR hub
is mapped at `/hubs/notifications`.

## Testing

Run unit and functional tests:

```bash
cd NotificationService.Tests
dotnet test --filter Category=Unit
dotnet test --filter Category=Functional
```

Functional tests spin up PostgreSQL and Redis via Testcontainers and require a running
Docker daemon.

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=coverage)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=flow-OverStack_NotificationService&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=flow-OverStack_NotificationService)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to your branch
5. Open a Pull Request

Please follow the existing code conventions and include tests for new functionality.
You are also welcome to open issues for bug reports, feature requests, or to discuss improvements.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/flow-OverStack/NotificationService/blob/master/LICENSE) file for details.

## Contact

For questions or suggestions open an issue.
