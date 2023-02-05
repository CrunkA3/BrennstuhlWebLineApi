namespace BrennstuhlWebLineApi;

using System.Net;
using System.Text;
public class Device : IDisposable
{

    public IPAddress IpAddress { get; }
    public IPAddress SubnetMask { get; }
    public IPAddress Gateway { get; }
    public short HttpPort { get; }
    public byte[] MacAddress { get; }
    public string Name { get; }

    public Uri BaseUri { get; set; }
    public CredentialCache CredentialCache { get; } = new CredentialCache();

    private DigestAuthenticationHandler? _httpClientHandler;
    private HttpClient? _webClient;

    private static TimeSpan MinRequestTimeSpan = TimeSpan.FromMilliseconds(2200);
    private DateTime _lastRequest = DateTime.Now;

    private Device(byte[] buffer)
    {
        using var ms = new MemoryStream(buffer);
        using var br = new BinaryReader(ms);

        var unknown1 = br.ReadInt32();
        var unknown2 = br.ReadInt32();

        IpAddress = new IPAddress(br.ReadBytes(4));
        SubnetMask = new IPAddress(br.ReadBytes(4));
        Gateway = new IPAddress(br.ReadBytes(4));
        HttpPort = br.ReadInt16();
        MacAddress = br.ReadBytes(6);

        //Version?
        var version = br.ReadBytes(4);

        Name = Encoding.UTF8.GetString(br.ReadBytes(64).TakeWhile(m => m != 0).ToArray());

        BaseUri = new Uri("http://" + IpAddress.ToString() + ":" + HttpPort);
    }

    public static Device ParseFromByteArray(byte[] data)
    {
        return new Device(data);
    }


    public void AddCredential(NetworkCredential credential)
    {
        CredentialCache.Add(BaseUri, "Digest", credential);
    }


    public Task<string> GetMainInformationAsync() => GetInformationAsync("bsMain");
    public Task<string> GetHeaderInformationAsync() => GetInformationAsync("bsHeader");
    public Task<string> GetHistoryInformationAsync() => GetInformationAsync("bsHistory");
    public Task<string> GetStateInformationAsync() => GetInformationAsync("bsState");
    public Task<string> GetSchedulerInformationAsync() => GetInformationAsync("bsScheduler");
    public Task<string> GetSetSwitchInformationAsync() => GetInformationAsync("bsSetSwitch");
    public Task<string> GetSystemSettingsInformationAsync() => GetInformationAsync("bsSysSet");
    public Task<string> GetDateTimeSettingsInformationAsync() => GetInformationAsync("bsDateTime");

    public async Task SetSwitchSetAsync(SetSwitchSettings settings)
    {
        string bootState0;
        switch (settings.BootState0)
        {
            case 2:
                bootState0 = "Last";
                break;
            case 1:
                bootState0 = "On";
                break;
            default:
                bootState0 = "Off";
                break;
        };

        string bootState1;
        switch (settings.BootState1)
        {
            case 2:
                bootState1 = "Last";
                break;
            case 1:
                bootState1 = "On";
                break;
            default:
                bootState1 = "Off";
                break;
        };

        var setRequestValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Name0", settings.ChannelName0 ?? string.Empty),
            new KeyValuePair<string, string>("Name1", settings.ChannelName1 ?? string.Empty),
            new KeyValuePair<string, string>("Btn0", bootState0),
            new KeyValuePair<string, string>("Btn1", bootState1),
            new KeyValuePair<string, string>("Sw0s", settings.Delay0.GetValueOrDefault(0).ToString()),
            new KeyValuePair<string, string>("Sw1s", settings.Delay1.GetValueOrDefault(0).ToString()),
            new KeyValuePair<string, string>("SwO0s", settings.Reset0.GetValueOrDefault(0).ToString()),
            new KeyValuePair<string, string>("SwO1s", settings.Reset1.GetValueOrDefault(0).ToString()),
            new KeyValuePair<string, string>("SUB", "Apply")
        };

        await SetAsync(setRequestValueCollection, "SwitchSet");
    }



    /// <summary>
    /// Toggle a relay
    /// </summary>
    /// <param name="relay">Relay-Index (must be 0 or 1)</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task ToggleRelayAsync(int relay)
    {
        if (relay < 0 || relay > 1) throw new ArgumentOutOfRangeException(nameof(relay));

        _webClient ??= EnsureWebClient();
        await EnsureRequestTimeSpan();

        using var deviceToggleResponse = await _webClient.GetAsync($"cgi/toggleRelay?Rel={relay}");
        deviceToggleResponse.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Get the states of the relays
    /// </summary>
    /// <returns>The states of both relays</returns>
    public async Task<RelayStates> GetRelayStatesAsync()
    {
        _webClient ??= EnsureWebClient();
        await EnsureRequestTimeSpan();

        using var deviceStateResponse = await _webClient.GetAsync("cgi/getRelays");
        deviceStateResponse.EnsureSuccessStatusCode();

        var setRelaysResponseContent = await deviceStateResponse.Content.ReadAsStringAsync();

        return new RelayStates(setRelaysResponseContent);
    }


    /// <summary>
    /// Set relay state
    /// </summary>
    /// <param name="relay"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public async Task SetRelayStateAsync(RelayNumber relay, RelayState state)
    {
        _webClient ??= EnsureWebClient();
        await EnsureRequestTimeSpan();

        using var deviceStateResponse = await _webClient.GetAsync($"cgi/setRelay?Rel={(int)relay}&State={(int)state}");
        deviceStateResponse.EnsureSuccessStatusCode();
    }



    private async Task SetAsync(IEnumerable<KeyValuePair<string, string>> setRequestValueCollection, string setting)
    {
        _webClient ??= EnsureWebClient();
        await EnsureRequestTimeSpan();

        var setRequestData = new FormUrlEncodedContent(setRequestValueCollection);

        using var deviceStateResponse = await _webClient.PostAsync("cgi/set" + setting, setRequestData);
        deviceStateResponse.EnsureSuccessStatusCode();
    }








    private Task<string> GetInformationAsync(string panel)
    {
        var deviceStateRequestValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("json", "on"),
            new KeyValuePair<string, string>("panel", panel)
        };

        return GetInformationAsync(deviceStateRequestValueCollection);
    }
    public async Task<string> GetInformationAsync(IEnumerable<KeyValuePair<string, string>> deviceStateRequestValueCollection)
    {
        _webClient ??= EnsureWebClient();
        await EnsureRequestTimeSpan();

        var deviceStateRequestData = new FormUrlEncodedContent(deviceStateRequestValueCollection);

        using var deviceStateResponse = await _webClient.PostAsync("cgi/getJsonData", deviceStateRequestData);
        deviceStateResponse.EnsureSuccessStatusCode();
        var stateContent = await deviceStateResponse.Content.ReadAsStringAsync();

        return stateContent;
    }



    private HttpClient EnsureWebClient()
    {
        _httpClientHandler ??= new DigestAuthenticationHandler() { Credentials = CredentialCache };
        _webClient ??= new HttpClient(_httpClientHandler) { BaseAddress = BaseUri };
        return _webClient;
    }
    private async Task EnsureRequestTimeSpan()
    {
        var timeSinceLastRequest = DateTime.Now - _lastRequest;
        if (timeSinceLastRequest < MinRequestTimeSpan) await Task.Delay(MinRequestTimeSpan - timeSinceLastRequest);
        _lastRequest = DateTime.Now;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_webClient != null)
            {
                _webClient.Dispose();
                _webClient = null;
            }

            if (_httpClientHandler != null)
            {
                _httpClientHandler.Dispose();
                _httpClientHandler = null;
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}