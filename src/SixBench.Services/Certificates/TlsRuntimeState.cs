namespace SixBench.Services.Certificates;

/// <summary>
/// How the server was bound at startup.
/// </summary>
/// <param name="HttpsEnabled">The port serves HTTPS.</param>
/// <param name="Port">The listening port.</param>
public sealed record TlsRuntimeState(bool HttpsEnabled, int Port);
