using Components.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promo.Worker.Domain.Services;

namespace Promo.Worker.Application;

public class ApplicationModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISendCampaignEmailsService, SendCampaignEmailsService>();
    }
}
