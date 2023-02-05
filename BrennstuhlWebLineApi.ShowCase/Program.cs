using System.Net;
using BrennstuhlWebLineApi;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

IEnumerable<Device> devices = await BrennstuhlWebLineFinder.FindAsync(m =>
                                        {
                                            Console.WriteLine(m.IpAddress.ToString());
                                        });


foreach (var device in devices)
{
    device.AddCredential(new NetworkCredential(config["username"], config["password"]));
    Console.WriteLine(await device.GetStateInformationAsync());
    Console.WriteLine(await device.GetHeaderInformationAsync());

    var relayStates = await device.GetRelayStatesAsync();
    Console.WriteLine("Relay0: {0} / Relay1: {1}", relayStates.Relay0, relayStates.Relay1);

    //await device.SetRelayStateAsync(RelayNumber.Relay1, RelayState.Off);
    //await Task.Delay(5000);
    //await device.SetRelayStateAsync(RelayNumber.Relay1, RelayState.On);
}