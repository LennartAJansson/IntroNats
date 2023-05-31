namespace NatsProducerApi.Services;


using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NATS.Extensions.DependencyInjection;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Func<IConfigurationSection> getSection)
    {
        IConfigurationSection section = getSection.Invoke();
        _ = services.AddNatsClient(options =>
        {
            NatsProducer natsProducer = section.Get<NatsProducer>()
                ?? throw new ArgumentException("You must add a proper NATS configuration");
            options.Servers = natsProducer.Servers;
            options.Url = natsProducer.Url;
            options.Verbose = true;
        });

        _ = services.Configure<NatsProducer>(section.Bind);
        _ = services.AddTransient<INatsService, NatsService>();
        return services;
    }
}

public class CustomJsonConverterForType : JsonConverter<Type>
{
    //WARNING! This is a breach in NET security recommendations regarding (de)serializing types
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? assemblyQualifiedName = reader.GetString();
        return Type.GetType(assemblyQualifiedName);
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        string assemblyQualifiedName = value.AssemblyQualifiedName;
        writer.WriteStringValue(assemblyQualifiedName);
    }
}