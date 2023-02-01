namespace BrennstuhlWebLineApi;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class DigestAuthenticationHandler : DelegatingHandler
{
    public ICredentials? Credentials { get; set; }

    private string? _realm;
    private string? _nonce;
    private string? _qop;
    private string? _cnonce;
    private string? _opaque;

    private DateTime _cnonceDate;
    private int _nc;


    public DigestAuthenticationHandler(ICredentials credentials)
    {
        base.InnerHandler = new HttpClientHandler();
        Credentials = credentials;
    }
    public DigestAuthenticationHandler()
    {
        base.InnerHandler = new HttpClientHandler();
    }


    private static string CalculateMd5Hash(string input)
    {
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hash = MD5.Create().ComputeHash(inputBytes);
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string GetHeaderParameter(string parameter, string? headerParameters)
    {
        if (string.IsNullOrEmpty(headerParameters)) return string.Empty;
        var regHeader = new Regex(string.Format(@"{0}=""([^""]*)""", parameter));
        var matchHeader = regHeader.Match(headerParameters);
        if (matchHeader.Success) return matchHeader.Groups[1].Value;
        throw new KeyNotFoundException(string.Format("Header {0} not found", parameter));
    }

    private string GetDigestHeader(Uri requestUri, NetworkCredential credential, string method)
    {
        var path = requestUri.PathAndQuery;

        _nc = _nc + 1;
        _cnonce = new Random().Next(1234500, 99999999).ToString();
        _cnonceDate = DateTime.Now;

        var ha1 = CalculateMd5Hash(string.Format("{0}:{1}:{2}", credential.UserName, _realm, credential.Password));
        var ha2 = CalculateMd5Hash(string.Format("{0}:{1}", method, path));
        var digestResponse =
            CalculateMd5Hash(string.Format("{0}:{1}:{2:X8}:{3}:{4}:{5}", ha1, _nonce, _nc, _cnonce, _qop, ha2));

        var result = string.Format("username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", " +
            "algorithm=MD5, response=\"{4}\", opaque=\"{5}\", qop={6}, nc={7:X8}, cnonce=\"{8}\"",
            credential.UserName, _realm, _nonce, path, digestResponse, _opaque, _qop, _nc, _cnonce);

        return result;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credential = Credentials?.GetCredential(request.RequestUri ?? new Uri("/"), "Digest");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        if (!string.IsNullOrEmpty(_cnonce) &&
            DateTime.Now.Subtract(_cnonceDate) < TimeSpan.FromHours(1) &&
            credential != null)
        {
            var digestHeader = GetDigestHeader(request.RequestUri ?? new Uri("/"), credential, request.Method.ToString());
            request.Headers.Authorization = new AuthenticationHeaderValue("Digest", digestHeader);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && Credentials != null)
        {
            var authHeader = response.Headers.WwwAuthenticate;
            if (authHeader == null || authHeader.Count == 0) return response;
            var firstAuthHeader = authHeader.First()!;

            if (credential == null) return response;

            _realm = GetHeaderParameter("realm", firstAuthHeader.Parameter);
            _nonce = GetHeaderParameter("nonce", firstAuthHeader.Parameter);
            _qop = GetHeaderParameter("qop", firstAuthHeader.Parameter);
            _opaque = GetHeaderParameter("opaque", firstAuthHeader.Parameter);

            _nc = 0;

            var digestHeader = GetDigestHeader(request.RequestUri ?? new Uri("/"), credential, request.Method.ToString());
            request.Headers.Authorization = new AuthenticationHeaderValue("Digest", digestHeader);
            response = await base.SendAsync(request, cancellationToken);

            authHeader = response.Headers.WwwAuthenticate;
        }

        return response;
    }
}