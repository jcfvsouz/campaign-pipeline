using Components.Hosting;
using Components.SQLServerRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promo.Worker.Domain.Email;
using Promo.Worker.Domain.Repositories;
using Promo.Worker.Infrastructure.Email;
using Promo.Worker.Infrastructure.Repositories;

namespace Promo.Worker.Infrastructure;

public class InfrastructureModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlServerRepository();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
    }
}
