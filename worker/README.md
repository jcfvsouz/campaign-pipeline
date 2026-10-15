# Promo.Worker

The Batch Email Dispatch side of the pipeline (see the [project root README](../README.md) for the full picture): a .NET Worker Service, in Clean Architecture, that consumes the `CampaignPublished` event published by [`Promo.Api`](../api/README.md) and sends a batch e-mail to every buyer in the campaign's buyer group.

## Why four projects for one event

Same discipline `api/` already demonstrates: **Domain** knows nothing outside itself, **Application** orchestrates but never touches SQL, SMTP, or a broker directly, **Infrastructure** is the only layer allowed to know Dapper, MailKit, or RabbitMQ exist, and the host wires everything together. Every dependency points inward.

This solution is independent of `api/` — it has no project reference to it, only a shared SQL Server database and a shared understanding of the `CampaignPublished` event's wire shape. `worker/` keeps its own copy of that event's contract, the same way `components/` packages are consumed as versioned NuGet rather than project references: each service owns its own code, sharing only what travels over the wire.

## Layout

```
worker/
├── Promo.Worker.slnx
├── Directory.Build.props          # net10.0, nullable/implicit usings — shared by every project below
├── nuget.config                   # nuget.org + ../components/nupkgs (this solution consumes those packages)
└── src/
    ├── Promo.Worker.Domain/
    │   ├── Entities/                   # Buyer
    │   ├── Repositories/               # IBuyerRepository (contract only)
    │   ├── Email/                      # EmailMessage, IEmailSender (contract only)
    │   ├── Services/                   # ISendCampaignEmailsService/SendCampaignEmailsService — the business rule
    │   └── AssemblyInfo.cs             # InternalsVisibleTo("Promo.Worker.Infrastructure") — see Infrastructure below
    ├── Promo.Worker.Application/
    │   ├── Events/                     # CampaignPublished — the event's wire contract
    │   ├── Handlers/                   # CampaignPublishedHandler — orchestration only
    │   └── ApplicationModule.cs        # IStartup — registers ISendCampaignEmailsService
    ├── Promo.Worker.Infrastructure/
    │   ├── Persistence/                # BuyerRow — see below for why it exists
    │   ├── Repositories/                # BuyerRepository (Dapper)
    │   ├── Email/                       # SmtpOptions, SmtpEmailSender (MailKit)
    │   └── InfrastructureModule.cs      # IStartup — registers SQL + repository + SMTP sender
    └── Promo.Worker/                    # Microsoft.NET.Sdk.Worker — see its own README
        ├── Program.cs
        └── appsettings.json
```

Every namespace mirrors its folder path 1:1, same convention `api/` and `components/` already use.

## Domain

`Buyer` — `Id`, `BuyerGroupId`, `Name`, `Email`, with a public constructor and `internal set` properties, same reasoning as `api/`'s `Campaign`/`BuyerGroup`: Infrastructure needs to rebuild a buyer's exact persisted state (`Id` included) on read, and `internal` plus `[assembly: InternalsVisibleTo("Promo.Worker.Infrastructure")]` keeps every other layer locked out of doing the same.

`IBuyerRepository` and `ISendCampaignEmailsService`/`SendCampaignEmailsService` live in Domain: a repository contract is phrased entirely in terms of the domain model, and `SendCampaignEmailsService` is a domain service enforcing the one real rule here — every buyer in the group gets the e-mail:

```csharp
public class SendCampaignEmailsService(IBuyerRepository buyerRepository, IEmailSender emailSender) : ISendCampaignEmailsService
{
    public async Task<int> SendAsync(Guid buyerGroupId, string campaignName, CancellationToken cancellationToken = default)
    {
        var buyers = await buyerRepository.GetByBuyerGroupIdAsync(buyerGroupId, cancellationToken);

        foreach (var buyer in buyers)
        {
            var message = new EmailMessage(buyer.Email, buyer.Name, $"New campaign: {campaignName}", $"Hi {buyer.Name}, check out our new campaign: {campaignName}!");
            await emailSender.SendAsync(message, cancellationToken);
        }

        return buyers.Count;
    }
}
```

It returns a plain `Task<int>` (the count sent), not `Result<int>`: there's no invariant here that can be legitimately violated — an empty buyer group just means zero e-mails sent, not a business-rule failure. Real failures (database unreachable, SMTP connection refused) are already exceptions from `IBuyerRepository`/`IEmailSender`, and are meant to propagate up to the consumer, which reacts to them (see Infrastructure/host below).

`IEmailSender`/`EmailMessage` live in Domain too, not Application: the Domain service shouldn't depend on an Application-level abstraction to fulfill its own rule — "a channel capable of notifying a buyer" is phrased purely in domain terms, with Infrastructure as the only implementation.

## Application

- `CampaignPublished` — the event's wire contract: `CampaignId`, `TenantId`, `BuyerGroupId`, `Name`, `PublishedAtUtc`. Deserialized with the default `System.Text.Json` settings (case-sensitive, PascalCase), matching exactly what `api/`'s publisher serializes.
- `CampaignPublishedHandler : IEventHandler<CampaignPublished>` — orchestration only: calls `ISendCampaignEmailsService.SendAsync` with the event's `BuyerGroupId`/`Name`, logs how many buyers were reached. It depends on neither `IBuyerRepository` nor `IEmailSender` directly, only the Domain service.
- `ApplicationModule : IStartup` — registers `ISendCampaignEmailsService -> SendCampaignEmailsService` (Domain's own type — Application owns this registration only because Domain has no `IStartup` of its own). The handler itself is registered by `Components.Messaging.RabbitMQ`'s `AddRabbitMqConsumer<TEvent,THandler>`, not by this module — see the host below.

## Infrastructure

`BuyerRepository` — a single Dapper query against [`ISqlRepository`](../components/src/Components.SQLRepository/README.md), the same database and the same abstraction `api/` uses.

Dapper's materialization needs a public parameterless constructor and public setters — `Buyer` deliberately has neither — so `BuyerRow` is the plain, publicly-settable shape Dapper reads into, with an **explicit conversion operator** to `Buyer`, same pattern as `api/`'s `CampaignRow`/`BuyerGroupRow`:

```csharp
public static explicit operator Buyer(BuyerRow row) =>
    new(row.BuyerGroupId, row.Name, row.Email) { Id = row.Id };
```

`SmtpEmailSender : IEmailSender` sends through [MailKit](https://github.com/jstedfast/MailKit)'s `SmtpClient` rather than the obsolete `System.Net.Mail.SmtpClient`, with no authentication — matching Mailhog, which accepts any connection.

`InfrastructureModule : IStartup` registers `AddSqlServerRepository()`, `IBuyerRepository -> BuyerRepository`, `IEmailSender -> SmtpEmailSender`, and binds `SmtpOptions` from configuration.

## The host

[`Promo.Worker`](src/Promo.Worker/README.md) is the only `Microsoft.NET.Sdk.Worker` project here — it wires Domain/Application/Infrastructure together and owns the RabbitMQ consumer registration. See its own README for the full detail (consumer wiring, configuration, and how to run it and exercise it end to end).

## Tests

Not yet added to this solution — same Moq + `IClassFixture<TFixture>` + `ConfigureMocks()`/`NewInstance()` + `Setup_{Dependency}_{Method}_Result(...)` pattern `api/` and `components/` already use will apply here too (`SendCampaignEmailsService`, `CampaignPublishedHandler`), see the [components root README](../components/README.md#tests) for the full rationale.
