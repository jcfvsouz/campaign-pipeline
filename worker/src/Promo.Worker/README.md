# Promo.Worker

The host — the only `Microsoft.NET.Sdk.Worker` project in this solution. It wires `Promo.Worker.Domain`, `Promo.Worker.Application`, and `Promo.Worker.Infrastructure` together into a running background service, and owns the one thing specific to being a message consumer: the RabbitMQ consumer registration. See the [worker root README](../../README.md) for the full Clean Architecture picture across all four projects.

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<ApplicationModule>();
builder.UseStartup<InfrastructureModule>();
builder.UseStartup<RabbitMqConsumerModule<CampaignPublished, CampaignPublishedHandler>>();

var host = builder.Build();
await host.RunAsync();
```

## Why `Program.cs` wires the consumer directly

`RabbitMqConsumerModule<TEvent, THandler>` is generic — it needs `CampaignPublished`/`CampaignPublishedHandler` known at compile time. Those types live in `Promo.Worker.Application`, which only the host references, so the host is the only place that can call `UseStartup<RabbitMqConsumerModule<CampaignPublished, CampaignPublishedHandler>>()`. `Promo.Worker.Infrastructure` can't own this registration the way it owns `AddSqlServerRepository()` — doing so would mean Infrastructure referencing Application's types, backwards from how dependencies are meant to point in this solution.

Registering it wires up, via `Components.Messaging.RabbitMQ`'s `AddRabbitMqConsumer<TEvent,THandler>`: the `RabbitMq` connection, a durable topic exchange, a per-queue dead-letter exchange + queue (failed messages are NACKed and routed there automatically instead of being retried forever or silently dropped), and `IEventHandler<CampaignPublished> -> CampaignPublishedHandler` itself.

## Configuration

`appsettings.json`:

- `ConnectionStrings:SqlServer` — the same database `Promo.Api` uses (run `api/database/schema.sql`/`seed.sql` once against it; this project adds no new tables).
- `RabbitMq` — same shape, same exchange (`campaign-pipeline`) as `Promo.Api`'s.
- `Consumers:CampaignPublished:{Destination, MessageKey, PrefetchCount}` — the queue this worker binds (`campaign-published-queue`), the routing key it listens for (`CampaignPublished`, matching what `Promo.Api` publishes with by default), and how many unacknowledged messages it'll hold at once.
- `Smtp:{Host, Port, From}` — points at Mailhog by default (`localhost:1025`, no auth).

There's no `docker/` compose file yet (a separate, still-planned piece of this project), so none of the above point at a running container until you stand them up yourself.

## Running it

```bash
cd worker
dotnet build Promo.Worker.slnx
dotnet run --project src/Promo.Worker
```

### Verified by actually running it

With no RabbitMQ broker reachable at the configured host/port, startup fails with `RabbitMQ.Client.Exceptions.BrokerUnreachableException` while resolving the hosted `EventConsumer<CampaignPublished>` — confirming the whole chain (`UseStartup<ApplicationModule>` → `UseStartup<InfrastructureModule>` → `UseStartup<RabbitMqConsumerModule<CampaignPublished, CampaignPublishedHandler>>` → DI resolution of `IEventConsumerStartup<CampaignPublished>`/`IConnection`) is wired correctly end to end, the same way `api/`'s host proves its own wiring by failing only at its database login. With a real broker reachable but no matching `CampaignPipeline` database or Mailhog instance, the failure instead surfaces later — inside `CampaignPublishedHandler.HandleAsync`, once an actual `CampaignPublished` message arrives — as a `SqlException` or an SMTP connection failure, which `RabbitMqConsumerStartup<TEvent>` catches, logs, and NACKs to the dead-letter queue rather than crashing the process.
