using System.Net.Http.Headers;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using System.Collections.Generic;
using System.Net.Http;
using Miniscript;
using System.Threading.Tasks;

namespace Kaolin.Flow.Plugins
{
    public class Http : Base
    {
        public Http(Engine engine) : base(engine)
        {
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        }

        readonly HttpClient client = new();
        public static Dictionary<string, string> UnWrapHeaders(ValMap headersMap)
        {
            Dictionary<string, string> headers = [];

            foreach (var entry in headersMap.map)
            {
                headers.Add(((ValString)entry.Key).value, ((ValString)entry.Value).value);
            }
            return headers;
        }

        public static ValList WrapData(byte[] bytes)
        {
            ValList data = new();

            foreach (var b in bytes)
            {
                data.values.Add(Utils.Cast(b));
            }

            return data;
        }
        public static byte[] UnWrapData(ValList bytesList)
        {
            List<byte> bytes = [];

            foreach (var b in bytesList.values)
            {
                bytes.Add((byte)b.IntValue());
            }

            return [.. bytes];
        }
        public static string UnWrapData(ValString str)
        {
            return str.value;
        }

        public static ValMap WrapHeaders(HttpContentHeaders headers)
        {
            MapBuilder headersBuilder = new();

            foreach (var entry in headers)
            {
                headersBuilder.AddProp(entry.Key, Utils.Cast(entry.Value.First()));
            }

            return headersBuilder.map;
        }
        public static async Task<ValMap> WrapResponse(HttpResponseMessage res)
        {
            ValMap headers = WrapHeaders(res.Content.Headers);

            byte[] bytes = await res.Content.ReadAsByteArrayAsync();
            ValList data = WrapData(bytes);

            ValMap map = new MapBuilder()
                .AddProp("headers", headers)
                .AddProp("data", data)
                .AddProp("statusCode", Utils.Cast((int)res.StatusCode))
                .AddProp("status", Utils.Cast(res.StatusCode.ToString()))
                .AddProp("uri", Utils.Cast(res.RequestMessage!.RequestUri!.AbsolutePath))
                .AddProp("toString",
                    new FunctionBuilder("toString")
                        .SetCallback((context, p) =>
                        {
                            return new Intrinsic.Result(System.Text.Encoding.UTF8.GetString(bytes));
                        })
                        .Function
                )
                .map;

            return map;
        }
        public ValFunction CreateComplexHTTPFunction(string name, HttpMethod method)
        {
            return new FunctionBuilder(name)
                .AddParam("url", new ValString("https://1.1.1.1"))
                .AddParam("data", new ValList())
                .AddParam("headers", new ValMap())
                .SetCallback((context, p) =>
                {
                    Dictionary<string, string> headers = UnWrapHeaders((ValMap)context.GetLocal("headers"));

                    var requestMessage = new HttpRequestMessage(method, context.GetLocalString("url"));

                    Value data = context.GetLocal("data");

                    if (data.GetType() == typeof(ValString)) requestMessage.Content = new StringContent(UnWrapData((ValString)data));
                    if (data.GetType() == typeof(ValList)) requestMessage.Content = new ByteArrayContent(UnWrapData((ValList)data));

                    return new Intrinsic.Result(
                        client.SendAsync(requestMessage)
                            .ContinueWith((t) =>
                            {
                                return WrapResponse(t.Result);
                            })
                            .GetAwaiter()
                            .GetResult()
                            .Result
                    );
                })
                .Function;
        }
        public ValFunction CreateSimpleHTTPFunction(string name, HttpMethod method)
        {
            return new FunctionBuilder(name)
                .AddParam("url", new ValString("https://1.1.1.1"))
                .AddParam("headers", new ValMap())
                .SetCallback((context, p) =>
                {
                    Dictionary<string, string> headers = UnWrapHeaders((ValMap)context.GetLocal("headers"));

                    var requestMessage =
    new HttpRequestMessage(method, context.GetLocalString("url"));

                    return new Intrinsic.Result(
                        client.SendAsync(requestMessage)
                            .ContinueWith((t) =>
                            {
                                return WrapResponse(t.Result);
                            })
                            .GetAwaiter()
                            .GetResult()
                            .Result
                    );
                })
                .Function;
        }
        public override void Inject()
        {
            ValMap map = new MapBuilder()
                .AddProp("get", CreateSimpleHTTPFunction("get", HttpMethod.Get))
                .AddProp("delete", CreateSimpleHTTPFunction("delete", HttpMethod.Delete))
                .AddProp("post", CreateComplexHTTPFunction("post", HttpMethod.Post))
                .AddProp("put", CreateComplexHTTPFunction("put", HttpMethod.Put))
                .map;

            Register("http", map);
        }
    }
}