using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class LanListener : MonoBehaviour
{
    UdpClient udp;
    IPEndPoint endpoint;

    public int port = 47777;

    private Menu menu;

    void Start()
    {
        endpoint = new IPEndPoint(IPAddress.Any, port);
        udp = new UdpClient(port);
        menu = GetComponent<Menu>();
        udp.BeginReceive(OnReceive, null);
    }

    void OnReceive(System.IAsyncResult ar)
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) return;
        byte[] data = udp.EndReceive(ar, ref endpoint);
        string message = Encoding.UTF8.GetString(data);

        if (message.StartsWith(Broadcaster.BroadcasterKey))
        {
            string ip = message.Split('|')[1];
            Debug.Log("Found server: " + ip);
            menu.OnIPReceived(ip);
        }
        udp.BeginReceive(OnReceive, null);
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}