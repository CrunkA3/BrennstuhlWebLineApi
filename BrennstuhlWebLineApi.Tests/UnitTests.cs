using Microsoft.Extensions.Configuration;

namespace BrennstuhlWebLineApi.Tests;

public class UnitTests
{
    IConfigurationRoot config;

    public UnitTests()
    {
        config = new ConfigurationBuilder()
            .AddUserSecrets<UnitTests>()
            .Build();
    }

    [Fact]
    public async void FindDeviceReturnsDevices()
    {
        var devices = await BrennstuhlWebLineFinder.FindAsync();
        Assert.NotEmpty(devices);
    }
}