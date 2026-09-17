namespace MediatR;

/// <summary>
/// Marker interface to represent a request with a streaming response
/// </summary>
/// <remarks>
/// TResponse is intentionally unused: this is a marker interface whose only purpose is to expose the response type.
/// </remarks>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IStreamRequest<out TResponse>;
