namespace NatsConsumerWorker;

using CloudNative.CloudEvents;

using Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NATS.Client;
using NATS.Client.JetStream;
using NATS.Extensions.DependencyInjection;

using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly NatsConsumer natsConsumer;
    private readonly IConnection connection;
    private IJetStream? jetStream;
    private IJetStreamPushAsyncSubscription? subscription;

    public Worker(ILogger<Worker> logger, IOptions<NatsConsumer> options, IConnection connection)
    {
        this.logger = logger;
        natsConsumer = options.Value;
        this.connection = connection;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        JetStreamUtils.CreateStreamWhenDoesNotExist(connection, natsConsumer.Stream, natsConsumer.Subject);

        ConsumerConfiguration cc = ConsumerConfiguration.Builder()
                                .WithDurable(natsConsumer.Consumer)
                                .WithDeliverSubject(natsConsumer.DeliverySubject)
                                .Build();

        _ = connection.CreateJetStreamManagementContext()
            .AddOrUpdateConsumer(natsConsumer.Stream, cc);

        jetStream = connection.CreateJetStreamContext();

        PushSubscribeOptions so = PushSubscribeOptions.BindTo(natsConsumer.Stream, natsConsumer.Consumer);
        subscription = jetStream.PushSubscribeAsync(natsConsumer.Subject, MessageArrived, true, so);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(5000, stoppingToken);
        }
    }


    private void MessageArrived(object? sender, MsgHandlerEventArgs args)
    {
        Msg msg = args.Message;
        if (msg is null || msg.Data is null)
        {
            return;
        }

        msg.Ack();

        logger.LogInformation("Received message {data} on subject {subject}, stream {stream}, seqno {seqno}.",
                        Encoding.UTF8.GetString(msg.Data), natsConsumer.Subject, msg.MetaData.Stream, msg.MetaData.StreamSequence);

        CloudEvent? evt = JsonSerializer.Deserialize<CloudEvent>(Encoding.UTF8.GetString(msg.Data));
        if (evt is null || evt.Data is null)
        {
            logger.LogError("Event is null or not a CloudEvent");
            return;
        }

        string status = "Received: ";

        string? json = evt.Data.ToString();

        if (evt.Data is null)
        {
            logger.LogError("Event Data is null and json is \"{json}\"", json);
            return;
        }

        NatsPostMessage? data = JsonSerializer.Deserialize<NatsPostMessage>((JsonElement)evt.Data);
        logger.LogInformation("{text}{data}", status, data?.Text);
    }
}
