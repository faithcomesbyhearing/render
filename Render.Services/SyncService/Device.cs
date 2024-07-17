using System.Net;

namespace Render.Services.SyncService
{
    public class Device
    {
        public IPAddress Address { get; init; }

        public string Name { get; init; }

        public bool IsConnected { get; set; }
        
        public Guid ProjectId { get; init; }
        
    }
}