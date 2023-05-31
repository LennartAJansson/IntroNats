using CloudNative.CloudEvents;

using System.Threading.Tasks;

namespace NatsProducerApi.Services
{
    public interface INatsService
    {
        Task SendAsync(CloudEvent cloudEvent);
    }
}