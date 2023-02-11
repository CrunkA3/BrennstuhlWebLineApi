using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;

namespace BrennstuhlWebLineApi;

public static class BrennstuhlWebLineFinder
{

    private static byte[] searchBytes = new byte[] { 0xff, 0x04, 0x02, 0xfb };
    private const int searchPort = 23;

    private static IPAddress GetBroadcastIP(IPAddress host, IPAddress mask)
    {
        byte[] broadcastIPBytes = new byte[4];
        byte[] hostBytes = host.GetAddressBytes();
        byte[] maskBytes = mask.GetAddressBytes();
        for (int i = 0; i < 4; i++)
        {
            broadcastIPBytes[i] = (byte)(hostBytes[i] | (byte)~maskBytes[i]);
        }
        return new IPAddress(broadcastIPBytes);
    }

    public static Task<IEnumerable<Device>> FindAsync()
    {
        return FindAsync(onFound: default);
    }
    public static Task<IEnumerable<Device>> FindAsync(Action<Device>? onFound)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return FindAsync(onFound, cancellationTokenSource.Token);
    }

    public static async Task<IEnumerable<Device>> FindAsync(Action<Device>? onFound, CancellationToken cancellationToken)
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        var networkInterfacesOnline = networkInterfaces
            .Where(m => m.OperationalStatus == OperationalStatus.Up && m.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            .ToArray();

        var foundDevices = new ConcurrentDictionary<string, Device>();
        var adressTasks = new List<Task>();
        Action<Device> onDeviceFound = (device) =>
            {
                if (foundDevices.TryAdd(Convert.ToHexString(device.MacAddress), device)) onFound?.Invoke(device);
            };

        foreach (var networkInterface in networkInterfacesOnline)
        {
            var addresses = networkInterface.GetIPProperties().UnicastAddresses
                .Where(m => m.Address.AddressFamily == AddressFamily.InterNetwork && !m.Address.IsIPv6Multicast)
                .ToArray();

            foreach (var address in addresses)
            {
                var task = FindAsync(onDeviceFound, address, cancellationToken);
                adressTasks.Add(task);
            }
        }


        try
        {
            await Task.WhenAll(adressTasks);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            throw;
        }

        return foundDevices.Values;
    }

    private static async Task FindAsync(Action<Device> onFound, UnicastIPAddressInformation addressInformation, CancellationToken cancellationToken)
    {
        var broadcastIPAddress = GetBroadcastIP(addressInformation.Address, addressInformation.IPv4Mask);
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        await socket.SendToAsync(searchBytes, SocketFlags.None, new IPEndPoint(broadcastIPAddress, searchPort));

        try
        {

            EndPoint responseEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] responseBytes = new byte[1024];
                int responseLength = (await socket.ReceiveFromAsync(responseBytes, SocketFlags.None, responseEndPoint)).ReceivedBytes;
                string response = Encoding.ASCII.GetString(responseBytes, 0, responseLength);

                var data = responseBytes[..responseLength];
                var device = Device.ParseFromByteArray(data);
                onFound.Invoke(device);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            socket.Close();
        }
    }
}