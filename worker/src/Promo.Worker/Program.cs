using Components.Hosting;
using Components.Messaging.RabbitMQ;
using Microsoft.Extensions.Hosting;
using Promo.Worker.Application;
using Promo.Worker.Application.Events;
using Promo.Worker.Application.Handlers;
using Promo.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<ApplicationModule>();
builder.UseStartup<InfrastructureModule>();
builder.UseStartup<RabbitMqConsumerModule<CampaignPublished, CampaignPublishedHandler>>();

var host = builder.Build();
await host.RunAsync();
