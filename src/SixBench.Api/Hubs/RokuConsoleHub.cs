using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SixBench.Common.Dtos;
using SixBench.Common.Security;
using SixBench.Services.Exceptions;
using SixBench.Services.Roku;

namespace SixBench.Api.Hubs;

/// <summary>
/// The BrightScript debug console of each Roku, shared by everyone watching it. Pushes
/// <c>ConsoleOutput</c> (<see cref="RokuConsoleOutputDto"/>) and <c>ConsoleStatus</c> (<see cref="RokuConsoleStatusDto"/>).
/// Admin only: the console can stop and step a running channel.
/// </summary>
/// <param name="consoles">Debug console connections.</param>
[Authorize(Roles = AppRoles.Admin)]
public sealed class RokuConsoleHub(IRokuDebugConsoleManager consoles) : Hub
{
    /// <summary>Hub path.</summary>
    public const string Path = "/hubs/roku-console";

    /// <summary>Output event name.</summary>
    public const string OutputEvent = "ConsoleOutput";

    /// <summary>Status event name.</summary>
    public const string StatusEvent = "ConsoleStatus";

    /// <summary>SignalR group of a Roku's console viewers.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>The group name.</returns>
    public static string Group(int rokuId) => $"roku-console-{rokuId}";

    /// <summary>Starts watching a Roku's console.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>Current state and recent output; later output with a higher sequence follows as events.</returns>
    public async Task<RokuConsoleSnapshotDto> Subscribe(int rokuId)
    {
        // Join the group first so no output falls between the snapshot and the events.
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(rokuId), Context.ConnectionAborted);
        try
        {
            return await consoles.SubscribeAsync(rokuId, Context.ConnectionId, Context.ConnectionAborted);
        }
        catch (NotFoundException ex)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(rokuId));
            throw new HubException(ex.Message);
        }
    }

    /// <summary>Stops watching a Roku's console.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>A task.</returns>
    public async Task Unsubscribe(int rokuId)
    {
        consoles.Unsubscribe(rokuId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(rokuId));
    }

    /// <summary>Types a line into a Roku's console.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <param name="line">The line.</param>
    /// <returns>A task.</returns>
    public async Task Send(int rokuId, string line)
    {
        try
        {
            await consoles.SendAsync(rokuId, line, Context.ConnectionAborted);
        }
        catch (RokuRequestRejectedException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        consoles.UnsubscribeAll(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// <see cref="IRokuConsoleNotifier"/> that broadcasts to a Roku's <see cref="RokuConsoleHub"/> group.
/// </summary>
/// <param name="hub">Hub context.</param>
public sealed class HubRokuConsoleNotifier(IHubContext<RokuConsoleHub> hub) : IRokuConsoleNotifier
{
    /// <inheritdoc />
    public Task OutputAsync(RokuConsoleOutputDto output) =>
        hub.Clients.Group(RokuConsoleHub.Group(output.RokuId)).SendAsync(RokuConsoleHub.OutputEvent, output);

    /// <inheritdoc />
    public Task StatusAsync(RokuConsoleStatusDto status) =>
        hub.Clients.Group(RokuConsoleHub.Group(status.RokuId)).SendAsync(RokuConsoleHub.StatusEvent, status);
}
