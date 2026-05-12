using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class Broadcaster : MonoBehaviour
{
    UdpClient udp;
    IPEndPoint endPoint;

    public int port = 47777;

    public const string BroadcasterKey = "MY_GAME_SERVER|";

    void Start()
    {
        udp = new UdpClient();
        udp.EnableBroadcast = true;

        endPoint = new IPEndPoint(IPAddress.Broadcast, port);
    }

    void Update()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        string message = BroadcasterKey + GetLocalIP();

        byte[] data = Encoding.UTF8.GetBytes(message);
        udp.Send(data, data.Length, endPoint);
    }

    string GetLocalIP()
    {
        return NetUtils.GetLocalIPAddress();
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}