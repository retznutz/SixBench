using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SixBench.Common.Dtos;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Where and how to reach a Roku's developer web server.
/// </summary>
/// <param name="Host">IP or host name.</param>
/// <param name="Port">Web server port (normally 80).</param>
/// <param name="UserName">User name (normally <c>rokudev</c>).</param>
/// <param name="Password">Developer-mode password.</param>
public sealed record RokuDevServerTarget(string Host, int Port, string UserName, string Password);

/// <summary>
/// A file produced by the Roku (screenshot or signed package).
/// </summary>
/// <param name="Content">File bytes.</param>
/// <param name="ContentType">Media type.</param>
/// <param name="FileName">Suggested download name.</param>
public sealed record RokuDevFile(byte[] Content, string ContentType, string FileName);

/// <summary>
/// The Roku developer-mode web server (port 80): the installer, packager and utilities pages.
/// Every call needs developer mode on and the developer password.
/// </summary>
public interface IRokuDevServerClient
{
    /// <summary>Checks that the password is accepted.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond (developer mode off?).</exception>
    /// <exception cref="RokuRequestRejectedException">The password was rejected.</exception>
    Task VerifyPasswordAsync(RokuDevServerTarget target, CancellationToken ct = default);

    /// <summary>Installs (or replaces) the sideloaded channel from a zip; the Roku then launches it.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="archive">Channel zip.</param>
    /// <param name="fileName">Zip file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused (bad password, compile error, corrupt zip, ...).</exception>
    Task<RokuDevActionResultDto> InstallAsync(RokuDevServerTarget target, byte[] archive, string fileName, CancellationToken ct = default);

    /// <summary>Deletes the sideloaded channel.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused.</exception>
    Task<RokuDevActionResultDto> DeleteAsync(RokuDevServerTarget target, CancellationToken ct = default);

    /// <summary>Converts the sideloaded channel to a squashfs image (faster to start, needed for large channels).</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused or the conversion failed.</exception>
    Task<RokuDevActionResultDto> ConvertToSquashfsAsync(RokuDevServerTarget target, CancellationToken ct = default);

    /// <summary>Takes a screenshot of the running sideloaded channel.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The image.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused or produced no screenshot.</exception>
    Task<RokuDevFile> ScreenshotAsync(RokuDevServerTarget target, CancellationToken ct = default);

    /// <summary>Packages the sideloaded channel into a signed <c>.pkg</c>, signed with the Roku's developer key.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="appName">Channel name.</param>
    /// <param name="version">Channel version.</param>
    /// <param name="signingPassword">Signing key password (from <c>genkey</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The package.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused (wrong signing password, no key, ...).</exception>
    Task<RokuDevFile> PackageAsync(RokuDevServerTarget target, string appName, string version, string signingPassword, CancellationToken ct = default);

    /// <summary>Re-keys the Roku with the developer key inside a previously signed package.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="package">A <c>.pkg</c> signed with the key to install.</param>
    /// <param name="fileName">Package file name.</param>
    /// <param name="signingPassword">That key's password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused.</exception>
    Task<RokuDevActionResultDto> RekeyAsync(RokuDevServerTarget target, byte[] package, string fileName, string signingPassword, CancellationToken ct = default);

    /// <summary>Reboots the Roku.</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused.</exception>
    Task<RokuDevActionResultDto> RebootAsync(RokuDevServerTarget target, CancellationToken ct = default);

    /// <summary>Asks the Roku to check for a software update (some Roku OS versions refuse installs until it has).</summary>
    /// <param name="target">The Roku.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="RokuUnreachableException">The web server did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused.</exception>
    Task<RokuDevActionResultDto> CheckForUpdateAsync(RokuDevServerTarget target, CancellationToken ct = default);
}

/// <summary>
/// Typed-<see cref="HttpClient"/> implementation of <see cref="IRokuDevServerClient"/>. Requests mirror the forms
/// on the Roku's web page (and the RokuCommunity <c>roku-deploy</c> tool).
/// </summary>
/// <param name="http">HTTP client (timeout configured at registration).</param>
/// <param name="time">Clock.</param>
/// <param name="logger">Logger; responses are logged at Debug so firmware differences can be diagnosed.</param>
public sealed class RokuDevServerClient(HttpClient http, TimeProvider time, ILogger<RokuDevServerClient>? logger = null)
    : IRokuDevServerClient
{
    private const int LoggedBodyChars = 4096;

    private const string InstallPath = "plugin_install";
    private const string InspectPath = "plugin_inspect";
    private const string PackagePath = "plugin_package";
    private const string SoftwareUpdatePath = "plugin_swup";

    /// <inheritdoc />
    public async Task VerifyPasswordAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        using var response = await SendAsync(target, HttpMethod.Get, InstallPath, form: null, ct);
        await EnsureSuccessAsync(target, response, InstallPath, ct);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> InstallAsync(
        RokuDevServerTarget target, byte[] archive, string fileName, CancellationToken ct = default)
    {
        // "Replace" also installs when nothing is there yet.
        var html = await PostFormAsync(
            target,
            InstallPath,
            [new("mysubmit", "Replace"), FormField.ForFile("archive", archive, fileName, "application/zip")],
            ct);

        if (html.Contains("Identical to previous version", StringComparison.OrdinalIgnoreCase))
        {
            return Result("This build is identical to the installed one, so the Roku kept it and relaunched it.", html);
        }

        if (CompileFailedRegex.IsMatch(html))
        {
            throw Rejected(html, "The channel failed to compile. Check the debug console for the errors.");
        }

        if (html.Contains("invalid or corrupt zip", StringComparison.OrdinalIgnoreCase))
        {
            throw Rejected(html, "The Roku says the zip is invalid or corrupt. The manifest must be at the root of the zip.");
        }

        ThrowOnErrors(html);
        return Result("Installed. The Roku is launching the channel.", html);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> DeleteAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        var html = await PostFormAsync(target, InstallPath, [new("mysubmit", "Delete"), new("archive", string.Empty)], ct);
        ThrowOnErrors(html);
        return Result("Sideloaded channel deleted.", html);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> ConvertToSquashfsAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        var html = await PostFormAsync(
            target, InstallPath, [new("mysubmit", "Convert to squashfs"), new("archive", string.Empty)], ct);
        if (!html.Contains("Conversion succeeded", StringComparison.OrdinalIgnoreCase))
        {
            throw Rejected(html, "The Roku did not confirm the squashfs conversion. Is a sideloaded channel installed?");
        }

        return Result("Converted the sideloaded channel to squashfs.", html);
    }

    /// <inheritdoc />
    public async Task<RokuDevFile> ScreenshotAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        var html = await PostFormAsync(
            target, InspectPath, [new("mysubmit", "Screenshot"), new("archive", string.Empty), new("passwd", string.Empty)], ct);
        ThrowOnErrors(html);
        var path = RokuDevServerParser.FindScreenshotPath(html)
            ?? throw Rejected(html, "The Roku did not produce a screenshot. It only captures a sideloaded channel that is running.");

        var bytes = await DownloadAsync(target, path, ct);
        var png = path.Contains(".png", StringComparison.OrdinalIgnoreCase);
        var stamp = time.GetLocalNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return new RokuDevFile(bytes, png ? "image/png" : "image/jpeg", $"roku-dev-{stamp}.{(png ? "png" : "jpg")}");
    }

    /// <inheritdoc />
    public async Task<RokuDevFile> PackageAsync(
        RokuDevServerTarget target, string appName, string version, string signingPassword, CancellationToken ct = default)
    {
        var html = await PostFormAsync(
            target,
            PackagePath,
            [
                new("mysubmit", "Package"),
                new("app_name", $"{appName}/{version}"),
                new("passwd", signingPassword),
                new("pkg_time", time.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)),
            ],
            ct);

        if (RokuDevServerParser.FindFontStatus(html) is { } status && status.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
        {
            throw Rejected(html, $"Packaging failed: {status}");
        }

        ThrowOnErrors(html);
        var path = RokuDevServerParser.FindPackagePath(html)
            ?? throw Rejected(html, "The Roku did not return a package. Check the signing password and that a sideloaded channel is installed.");

        var bytes = await DownloadAsync(target, path, ct);
        return new RokuDevFile(bytes, "application/octet-stream", $"{SafeFileName(appName)}_{version}.pkg");
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> RekeyAsync(
        RokuDevServerTarget target, byte[] package, string fileName, string signingPassword, CancellationToken ct = default)
    {
        var html = await PostFormAsync(
            target,
            InspectPath,
            [
                new("mysubmit", "Rekey"),
                new("passwd", signingPassword),
                FormField.ForFile("archive", package, fileName, "application/octet-stream"),
            ],
            ct);

        var status = RokuDevServerParser.FindFontStatus(html);
        if (!string.Equals(status, "Success.", StringComparison.OrdinalIgnoreCase))
        {
            throw Rejected(html, status is null ? "The Roku did not confirm the rekey." : $"Rekey failed: {status}");
        }

        return Result("The Roku is now keyed with the package's developer key.", html);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> RebootAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        var html = await PostFormAsync(target, SoftwareUpdatePath, [new("mysubmit", "Reboot")], ct);
        ThrowOnErrors(html);
        return Result("The Roku is rebooting.", html);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> CheckForUpdateAsync(RokuDevServerTarget target, CancellationToken ct = default)
    {
        var html = await PostFormAsync(target, SoftwareUpdatePath, [new("mysubmit", "CheckUpdate")], ct);
        ThrowOnErrors(html);
        return Result("The Roku checked for a software update.", html);
    }

    /// <summary>
    /// Builds the multipart body the Roku's forms send: quoted part names and an unquoted boundary, like a browser.
    /// </summary>
    /// <param name="fields">Form fields.</param>
    /// <returns>The body.</returns>
    public static MultipartFormDataContent BuildForm(IReadOnlyList<FormField> fields)
    {
        var boundary = "----SixBench" + Guid.NewGuid().ToString("N");
        var form = new MultipartFormDataContent(boundary);
        form.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={boundary}");
        foreach (var field in fields)
        {
            HttpContent part;
            if (field.File is { } bytes)
            {
                part = new ByteArrayContent(bytes);
                part.Headers.ContentType = new MediaTypeHeaderValue(field.FileContentType ?? "application/octet-stream");
            }
            else
            {
                part = new StringContent(field.Value ?? string.Empty);
                part.Headers.ContentType = null;
            }

            part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{field.Name}\"",
                FileName = field.File is null ? null : $"\"{SafeFileName(field.FileName ?? "upload")}\"",
            };
            form.Add(part);
        }

        return form;
    }

    private async Task<string> PostFormAsync(RokuDevServerTarget target, string path, IReadOnlyList<FormField> fields, CancellationToken ct)
    {
        using var response = await SendAsync(target, HttpMethod.Post, path, fields, ct);
        await EnsureSuccessAsync(target, response, path, ct);
        var html = await RokuEcpClient.ExecuteAsync(target.Host, () => response.Content.ReadAsStringAsync(ct));
        logger?.LogDebug(
            "Roku {Host} /{Path} ({Submit}) answered HTTP {Status}: {Body}",
            target.Host,
            path,
            fields.FirstOrDefault(f => f.Name == "mysubmit")?.Value,
            (int)response.StatusCode,
            Excerpt(html));
        return html;
    }

    private async Task<byte[]> DownloadAsync(RokuDevServerTarget target, string path, CancellationToken ct)
    {
        using var response = await SendAsync(target, HttpMethod.Get, path.TrimStart('/'), form: null, ct);
        await EnsureSuccessAsync(target, response, path, ct);
        return await RokuEcpClient.ExecuteAsync(target.Host, () => response.Content.ReadAsByteArrayAsync(ct));
    }

    /// <summary>
    /// Sends with Digest auth. A body-less request first fetches the challenge so a large upload is sent once.
    /// An authenticated request that still gets 401 is retried once with the fresh challenge (some servers reject
    /// a nonce without flagging it stale); only a second authenticated 401 means the password is wrong.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        RokuDevServerTarget target, HttpMethod method, string path, IReadOnlyList<FormField>? form, CancellationToken ct)
    {
        var uri = new Uri($"http://{FormatHost(target.Host)}:{target.Port}/{path}");

        IReadOnlyDictionary<string, string>? challenge = null;
        if (form is not null)
        {
            using var probe = await ExecuteAsync(target, new HttpRequestMessage(HttpMethod.Get, uri), ct);
            if (probe.StatusCode == HttpStatusCode.Unauthorized)
            {
                challenge = HttpDigest.FindChallenge(probe.Headers.WwwAuthenticate);
            }
        }

        var authenticatedAttempts = 0;
        while (true)
        {
            var request = new HttpRequestMessage(method, uri) { Content = form is null ? null : BuildForm(form) };
            if (challenge is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Digest",
                    HttpDigest.CreateAuthorization(
                        challenge, method.Method, uri.PathAndQuery, target.UserName, target.Password, HttpDigest.NewClientNonce()));
                authenticatedAttempts++;
            }

            var response = await ExecuteAsync(target, request, ct);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            var next = HttpDigest.FindChallenge(response.Headers.WwwAuthenticate);
            if (next is null || authenticatedAttempts >= 2)
            {
                return response;
            }

            response.Dispose();
            challenge = next;
        }
    }

    private async Task<HttpResponseMessage> ExecuteAsync(RokuDevServerTarget target, HttpRequestMessage request, CancellationToken ct)
    {
        using (request)
        {
            try
            {
                return await RokuEcpClient.ExecuteAsync(target.Host, () => http.SendAsync(request, ct));
            }
            catch (RokuUnreachableException ex)
            {
                throw new RokuUnreachableException(
                    $"{ex.Message} The developer web server (port {target.Port}) only runs while developer mode is enabled on the Roku.",
                    ex.InnerException ?? ex);
            }
        }
    }

    private async Task EnsureSuccessAsync(RokuDevServerTarget target, HttpResponseMessage response, string path, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger?.LogDebug(
                "Roku {Host} /{Path} answered HTTP {Status}; WWW-Authenticate: {Challenge}; body: {Body}",
                target.Host,
                path.TrimStart('/'),
                (int)response.StatusCode,
                string.Join(" | ", response.Headers.WwwAuthenticate),
                Excerpt(await response.Content.ReadAsStringAsync(ct)));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new RokuDevPasswordRejectedException(
                $"Roku {target.Host} rejected the developer password. Update it in Settings > Rokus " +
                "(it is the password chosen when developer mode was enabled).");
        }

        if ((int)response.StatusCode == 577)
        {
            throw new RokuRequestRejectedException(
                $"Roku {target.Host} needs to check for a software update before it accepts this (HTTP 577). Use Check for update, then try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var errors = RokuDevServerParser.ParseMessages(body).Where(m => m.Type == RokuDevServerParser.Error).Select(m => m.Text).ToList();
            throw new RokuRequestRejectedException(
                $"Roku {target.Host} answered /{path.TrimStart('/')} with HTTP {(int)response.StatusCode}." +
                (errors.Count > 0 ? " " + string.Join(" ", errors) : string.Empty));
        }
    }

    private static void ThrowOnErrors(string html)
    {
        var errors = RokuDevServerParser.ParseMessages(html).Where(m => m.Type == RokuDevServerParser.Error).ToList();
        if (errors.Count > 0)
        {
            throw new RokuRequestRejectedException(string.Join(" ", errors.Select(e => e.Text)));
        }

        if (UpdateCheckRegex.IsMatch(html))
        {
            throw new RokuRequestRejectedException(
                "The Roku needs to check for a software update before it accepts this. Use Check for update, then try again.");
        }
    }

    private static RokuRequestRejectedException Rejected(string html, string fallback)
    {
        var errors = RokuDevServerParser.ParseMessages(html).Where(m => m.Type == RokuDevServerParser.Error).Select(m => m.Text).ToList();
        return new RokuRequestRejectedException(errors.Count > 0 ? string.Join(" ", errors) : fallback);
    }

    private static RokuDevActionResultDto Result(string summary, string html) => new(summary, RokuDevServerParser.ParseMessages(html));

    private static string Excerpt(string text) => text.Length <= LoggedBodyChars ? text : text[..LoggedBodyChars] + "…";

    private static string FormatHost(string host) => host.Contains(':', StringComparison.Ordinal) && !host.StartsWith('[') ? $"[{host}]" : host;

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['"', '\\', '/']).ToHashSet();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) || !char.IsAscii(c) ? '_' : c).ToArray()).Trim();
        return cleaned.Length == 0 ? "channel" : cleaned;
    }

    private static readonly System.Text.RegularExpressions.Regex CompileFailedRegex =
        new(@"install\sfailure:\scompilation\sfailed", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex UpdateCheckRegex =
        new(@"[""']\s*Failed\s*to\s*check\s*for\s*software\s*update\s*[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

/// <summary>
/// One field of a developer web server form.
/// </summary>
/// <param name="Name">Field name.</param>
/// <param name="Value">Text value (ignored for files).</param>
public sealed record FormField(string Name, string? Value)
{
    /// <summary>File contents, for a file field.</summary>
    public byte[]? File { get; init; }

    /// <summary>File name, for a file field.</summary>
    public string? FileName { get; init; }

    /// <summary>File media type, for a file field.</summary>
    public string? FileContentType { get; init; }

    /// <summary>Creates a file field.</summary>
    /// <param name="name">Field name.</param>
    /// <param name="content">File bytes.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="contentType">Media type.</param>
    /// <returns>The field.</returns>
    public static FormField ForFile(string name, byte[] content, string fileName, string contentType) =>
        new(name, null) { File = content, FileName = fileName, FileContentType = contentType };
}
