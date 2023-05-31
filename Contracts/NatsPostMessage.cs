namespace Contracts;

public record NatsPostMessage(string Text)
{
    public static NatsPostMessage Instance(string text) => new(text);
}