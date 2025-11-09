// Hubs/ZeynAIHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class ZeynAIHub : Hub
{
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    internal static string GroupName(Guid conversationId) => $"chat:{conversationId}";
}