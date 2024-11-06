using System.Net.Sockets;

namespace Render.Services.SyncService;

public class ConnectedDevice : Device
{
    private readonly TcpClient _tcpClient;
    public ConnectionTask ConnectionTask { get; set; }

    public ConnectedDevice(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    public Stream GetStream()
    {
        return _tcpClient.Connected ? _tcpClient?.GetStream() : null;
    }

    public bool IsClientConnected()
    {
        return _tcpClient.Connected;
    }
    
    public void CloseClient()
    {
        _tcpClient?.Close();
    }
}