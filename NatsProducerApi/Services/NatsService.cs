namespace NatsProducerApi.Services;

using CloudNative.CloudEvents;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NATS.Client;
using NATS.Client.JetStream;
using NATS.Extensions.DependencyInjection;

using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class NatsService : INatsService
{
    private readonly ILogger<NatsService> logger;
    protected readonly NatsProducer natsProducer;
    private readonly IConnection connection;
    private readonly IJetStream jetStream;

    public NatsService(ILogger<NatsService> logger, IConnection connection, IOptions<NatsProducer> options)
    {
        this.logger = logger;
        this.connection = connection;
        natsProducer = options.Value;
        _ = JetStreamUtils.CreateStreamOrUpdateSubjects(this.connection, natsProducer.Stream, natsProducer.Subject);
        jetStream = connection.CreateJetStreamContext();
    }

    public async Task SendAsync(CloudEvent cloudEvent)
    {
        connection.ResetStats();
        JsonSerializerOptions options = new();
        options.Converters.Add(new CustomJsonConverterForType());
        string json = JsonSerializer.Serialize(cloudEvent, options);

        //Msg msg = await connection.RequestAsync($"{cloudEvent.Subject}.{cloudEvent.Id}", Encoding.UTF8.GetBytes(json), 10000);
        //connection.Publish($"{cloudEvent.Subject}.{cloudEvent.Id}", Encoding.UTF8.GetBytes(json));
        PublishAck ack = jetStream.Publish($"{cloudEvent.Subject}.{cloudEvent.Id}", Encoding.UTF8.GetBytes(json));

        //string decodedResponse = Encoding.UTF8.GetString(msg.Data);
        logger.LogInformation("Response from NATS is: {code} - {description}", ack.ErrorCode, ack.ErrorDescription);
    }
}