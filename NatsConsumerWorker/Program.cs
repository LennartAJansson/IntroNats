namespace NatsConsumerWorker;

using NATS.Extensions.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                _ = services.Configure<NatsConsumer>(options => context.Configuration.GetSection("NATS").Bind(options));
                _ = services.AddNatsClient(options =>
                {
                    NatsConsumer natsConsumer = context.Configuration.GetSection("NATS").Get<NatsConsumer>()
                        ?? throw new ArgumentException("You must add a proper NATS configuration");
                    options.Servers = natsConsumer.Servers;
                    options.Url = natsConsumer.Url;
                    options.Verbose = true;
                });
                _ = services.AddHostedService<Worker>();
            })
            .Build();

        host.Run();
    }
}