using Microsoft.AspNetCore.SignalR;

namespace TourOperator.Application.Hubs
{
    public class UploadProgressHub : Hub
    {
        // Clients will call /negotiate to get connection id; client must provide connection id in upload request
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
    }
}
