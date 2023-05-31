using CloudNative.CloudEvents;

using Contracts;

using Microsoft.Extensions.Options;

using NATS.Extensions.DependencyInjection;

using NatsProducerApi.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

builder.Services.AddApplicationServices(() => builder.Configuration.GetSection("NATS"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}
_ = app.UseSwagger();
_ = app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapPost("/natsmessage", async (NatsPostMessage request, HttpContext httpContext, INatsService service, IOptions<NatsProducer> options) =>
{
    string scheme = httpContext.Request.Scheme;
    string host = httpContext.Request.Host.Value;
    string path = httpContext.Request.Path.Value;
    string url = $"{scheme}://{host}{path}";
    CloudEvent evt = new(CloudEventsSpecVersion.Default)
    {
        Id = $"{DateTimeOffset.Now.Ticks}",
        Source = new Uri(url),
        Time = DateTimeOffset.Now,
        Type = nameof(NatsPostMessage),
        DataContentType = "application/json; charset=UTF-8",
        Subject = options.Value.Subject,
        Data = request
    };
    await service.SendAsync(evt);
})
.WithName("PostNatsMessage")
.WithOpenApi();

app.Run();
