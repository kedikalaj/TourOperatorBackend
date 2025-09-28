using Microsoft.AspNetCore.SignalR;

namespace TourOperator.Application.Hubs
{
    public class UploadProgressHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            // optional logging
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }

}
