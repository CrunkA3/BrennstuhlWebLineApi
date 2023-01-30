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


    public Task<string> GetHeaderInformationAsync()
    {
        var deviceStateRequestValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("json", "on"),
            new KeyValuePair<string, string>("panel", "bsHeader"),
        };

        return GetInformationAsync(deviceStateRequestValueCollection);
    }

    public Task<string> GetStateInformationAsync()
    {
        var deviceStateRequestValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("json", "on"),
            new KeyValuePair<string, string>("panel", "bsState")
        };

        return GetInformationAsync(deviceStateRequestValueCollection);
    }

    public async Task<string> GetInformationAsync(IEnumerable<KeyValuePair<string, string>> deviceStateRequestValueCollection)
    {
        _httpClientHandler ??= new DigestAuthenticationHandler() { Credentials = CredentialCache };
        _webClient ??= new HttpClient(_httpClientHandler) { BaseAddress = BaseUri };

        var deviceStateRequestData = new FormUrlEncodedContent(deviceStateRequestValueCollection);
        var deviceStateResponse = await _webClient.PostAsync("cgi/getJsonData", deviceStateRequestData);
        var responseHeaders = deviceStateResponse.Content.Headers;
        deviceStateResponse.EnsureSuccessStatusCode();
        var stateContent = await deviceStateResponse.Content.ReadAsStringAsync();

        return stateContent;
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