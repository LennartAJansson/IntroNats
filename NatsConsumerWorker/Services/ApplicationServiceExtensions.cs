namespace NatsConsumerWorker.Services;


using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NATS.Extensions.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Func<IConfigurationSection> getSection)
    {
        IConfigurationSection section = getSection.Invoke();
        _ = services.AddNatsClient(options =>
        {
            NatsConsumer natsProducer = section.Get<NatsConsumer>()
                ?? throw new ArgumentException("You must add a proper NATS configuration");
            options.Servers = natsProducer.Servers;
            options.Url = natsProducer.Url;
            options.Verbose = true;
        });

        _ = services.Configure<NatsConsumer>(section.Bind);
        return services;
    }
}
