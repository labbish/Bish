using System.Net.Http.Headers;
using BishRuntime;

namespace BishLib;

public struct BishHttpModule : IModule
{
    public static BishObject Exports => IModule.ExportsFrom(
        ("Client", BishHttpClient.StaticType),
        ("Response", BishHttpResponse.StaticType),
        ("HttpError", Error)
    );

    public static readonly BishType Error = new("HttpError", [BishError.StaticType]);
}

public class BishHttpClient(HttpClient client) : BishObject
{
    public readonly HttpClient Client = client;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Client");

    [Builtin("hook")]
    public static BishHttpClient New() => new(new HttpClient());

    [Builtin]
    public static void Dispose(BishHttpClient self) => self.Client.Dispose();

    /*
     * Options {
     *     method: string?      ('GET')
     *     body: string?        (empty)
     *     encoding: string?    ('utf-8')
     *     mediaType: string?   ('text/plain')
     *     headers: Map<string, string|string[]>?
     * }
     */
    [Builtin]
    public static BishNativeTask Fetch(BishHttpClient self, BishString url, [DefaultNull] BishObject? options) =>
        new(async () => new BishHttpResponse(await BishException
            .Wrapped(BishHttpModule.Error, self.Fetch(url.Value, options))));

    public async Task<HttpResponseMessage> Fetch(string url, BishObject? options = null)
    {
        using var request = new HttpRequestMessage(ParseMethod(options), url);
        ParseHeadersTo(options, request.Headers);
        if (ParseContent(options) is { } content) request.Content = content;
        return await Client.SendAsync(request);
    }

    private static HttpMethod ParseMethod(BishObject? options) => new(options.GetString("method") ?? "GET");

    private static StringContent? ParseContent(BishObject? options)
    {
        if (options.GetString("body") is not { } body) return null;
        var encoding = BishFileModule.EncodingFrom(options.GetString("encoding"));
        var mediaType = options.GetString("mediaType");
        return new StringContent(body, encoding, mediaType);
    }

    private static void ParseHeadersTo(BishObject? options, HttpRequestHeaders headers)
    {
        if (options.Get<BishMap>("headers") is not { } map) return;
        foreach (var (key, value) in map.Entries)
            switch (value)
            {
                case BishString str: headers.Add(key.As<BishString>("key").Value, str.Value); break;
                case BishList list:
                    headers.Add(key.As<BishString>("key").Value,
                        list.List.Select(item => item.As<BishString>("item").Value)); break;
                default: throw BishException.OfType_Expect("value", value, "string or list of string");
            }
    }
}

internal static class OptionsHelper
{
    extension(BishObject? options)
    {
        public T? Get<T>(string key) where T : BishObject => options?.TryGetMember(key)?.As<T>(key);

        public string? GetString(string key) => options.Get<BishString>(key)?.Value;
    }
}

public class BishHttpResponse(HttpResponseMessage message) : BishObject
{
    public readonly HttpResponseMessage Message = message;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Response");

    [Builtin]
    public static void Dispose(BishHttpResponse self) => self.Message.Dispose();

    [Builtin("hook")]
    public static BishInt Get_status(BishHttpResponse self) => BishInt.Of((int)self.Message.StatusCode);

    [Builtin("hook")]
    public static BishBool Get_success(BishHttpResponse self) => BishBool.Of(self.Message.IsSuccessStatusCode);

    [Builtin("hook")]
    public static BishNativeTask Get_content(BishHttpResponse self) => new(async () =>
        new BishString(await BishException.Wrapped(BishHttpModule.Error,
            self.Message.Content.ReadAsStringAsync())));
}