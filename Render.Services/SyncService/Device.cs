using System.Net;

namespace Render.Services.SyncService
{
    public class Device
    {
        public IPAddress Address { get; init; }

        public string Name { get; set; }

        public bool IsConnected { get; set; }
        
        public Guid ProjectId { get; set; }
        
    }
}