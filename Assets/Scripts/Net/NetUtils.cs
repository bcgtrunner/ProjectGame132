using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

public static class NetUtils
{
    public static List<string> GetIps()
    {
        List<string> ips = new();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up || ni.Description.Contains("Virtual"))
                continue;

            if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                continue;


            var props = ni.GetIPProperties();

            foreach (var ip in props.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip.Address))
                {
                    ips.Add(ip.Address.ToString());
                }
            }
        }
        if (ips.Count == 0) ips.Add("127.0.0.1");
        return ips;
    }

    public static bool IsIPCorrect(string ip)
    {
        string[] nums = ip.Split('.');
        if (nums.Length != 4) return false;
        foreach (var item in nums)
        {
            if (int.TryParse(item, out var n))
            {
                if (n < 0 || n > 255) return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public static string GetLocalIPAddress()
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }

        return "127.0.0.1";
    }
}
