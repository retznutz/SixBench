using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SixBench.Common.Dtos;
using SixBench.Services.Exceptions;
using SixBench.Services.Roku;

namespace SixBench.Api.Hubs;

/// <summary>
/// Live state of a Roku for the remote control. Pushes <c>TextEditState</c> (<see cref="RokuTextEditStateDto"/>)
/// when a text field on the Roku opens or closes, so the browser can open its text box. Any signed-in user.
/// </summary>
/// <param name="textEdit">Keyboard monitor.</param>
[Authorize]
public sealed class RokuRemoteHub(IRokuTextEditMonitor textEdit) : Hub
{
    /// <summary>Hub path.</summary>
    public const string Path = "/hubs/roku-remote";

    /// <summary>Keyboard state event name.</summary>
    public const string TextEditStateEvent = "TextEditState";

    /// <summary>SignalR group of a Roku's viewers.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>The group name.</returns>
    public static string Group(int rokuId) => $"roku-remote-{rokuId}";

    /// <summary>Starts watching a Roku.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>The current keyboard state; changes follow as events.</returns>
    public async Task<RokuTextEditStateDto> Subscribe(int rokuId)
    {
        // Join the group first so no change falls between the snapshot and the events.
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(rokuId), Context.ConnectionAborted);
        try
        {
            return await textEdit.SubscribeAsync(rokuId, Context.ConnectionId, Context.ConnectionAborted);
        }
        catch (NotFoundException ex)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(rokuId));
            throw new HubException(ex.Message);
        }
    }

    /// <summary>Stops watching a Roku.</summary>
    /// <param name="rokuId">Roku id.</param>
    /// <returns>A task.</returns>
    public async Task Unsubscribe(int rokuId)
    {
        textEdit.Unsubscribe(rokuId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(rokuId));
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        textEdit.UnsubscribeAll(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// <see cref="IRokuTextEditNotifier"/> that broadcasts to a Roku's <see cref="RokuRemoteHub"/> group.
/// </summary>
/// <param name="hub">Hub context.</param>
public sealed class HubRokuTextEditNotifier(IHubContext<RokuRemoteHub> hub) : IRokuTextEditNotifier
{
    /// <inheritdoc />
    public Task StateAsync(RokuTextEditStateDto state) =>
        hub.Clients.Group(RokuRemoteHub.Group(state.RokuId)).SendAsync(RokuRemoteHub.TextEditStateEvent, state);
}
