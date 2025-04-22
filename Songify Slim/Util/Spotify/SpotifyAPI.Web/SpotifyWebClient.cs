using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using YamlDotNet.Core.Tokens;
using Error = Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models.Error;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web
{
    internal class SpotifyWebClient : IClient
    {
        public JsonSerializerSettings JsonSettings { get; set; }
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly HttpClient _client;

        private const string UnknownErrorJson = "{\"error\": { \"status\": 0, \"message\": \"{0}\" }}";

        public SpotifyWebClient(ProxyConfig proxyConfig = null)
        {
            HttpClientHandler clientHandler = ProxyConfig.CreateClientHandler(proxyConfig);
            _client = new HttpClient(clientHandler);
        }

        public Tuple<ResponseInfo, string> Download(string url, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, byte[]> raw = DownloadRaw(url, headers);
            return new Tuple<ResponseInfo, string>(raw.Item1, raw.Item2.Length > 0 ? _encoding.GetString(raw.Item2) : "{}");
        }

        public async Task<Tuple<ResponseInfo, string>> DownloadAsync(string url, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, byte[]> raw = await DownloadRawAsync(url, headers).ConfigureAwait(false);
            return new Tuple<ResponseInfo, string>(raw.Item1, raw.Item2.Length > 0 ? _encoding.GetString(raw.Item2) : "{}");
        }

        public Tuple<ResponseInfo, byte[]> DownloadRaw(string url, Dictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                AddHeaders(headers);
            }

            using HttpResponseMessage response = Task.Run(() => _client.GetAsync(url)).Result;
            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
            {
                StatusCode = response.StatusCode,
                Headers = ConvertHeaders(response.Headers)
            }, Task.Run(() => response.Content.ReadAsByteArrayAsync()).Result);
        }

        public async Task<Tuple<ResponseInfo, byte[]>> DownloadRawAsync(string url, Dictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                AddHeaders(headers);
            }

            using HttpResponseMessage response = await _client.GetAsync(url).ConfigureAwait(false);
            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
            {
                StatusCode = response.StatusCode,
                Headers = ConvertHeaders(response.Headers)
            }, await response.Content.ReadAsByteArrayAsync());
        }

        public Tuple<ResponseInfo, T> DownloadJson<T>(string url, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, string> response = Download(url, headers);
            try
            {
                return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
            }
            catch (JsonException error)
            {
                Logger.LogStr("SPOTIFY API:" + url);
                if (!File.Exists("data.json"))
                    File.Create("data.json").Close();
                File.WriteAllText("data.json", response.Item2);
                Logger.LogExc(error);
                IoManager.WriteOutput($"{GlobalObjects.RootDirectory}/json.txt", response.Item2);
                return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(string.Format(UnknownErrorJson, error.Message), JsonSettings));
            }
        }

        public async Task<Tuple<ResponseInfo, T>> DownloadJsonAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, string> response = await DownloadAsync(url, headers).ConfigureAwait(false);

            try
            {
                return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
            }
            catch (JsonException error)
            {
                Logger.LogStr("SPOTIFY API:" + url);
                Logger.LogExc(error);
                IoManager.WriteOutput($"{GlobalObjects.RootDirectory}/json.txt", response.Item2);

                string safeErrorMessage = error.Message
                    .Replace("{", "{{")
                    .Replace("}", "}}");

                Logger.LogStr($"SPOTIFY API: {safeErrorMessage}");

                return new Tuple<ResponseInfo, T>(
                    response.Item1,
                    JsonConvert.DeserializeObject<T>(string.Format(UnknownErrorJson, safeErrorMessage), JsonSettings)
                );

                //return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(string.Format(UnknownErrorJson, error.Message), JsonSettings));
            }
        }

        public Tuple<ResponseInfo, string> Upload(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, byte[]> data = UploadRaw(url, body, method, headers);
            return new Tuple<ResponseInfo, string>(data.Item1, data.Item2.Length > 0 ? _encoding.GetString(data.Item2) : "{}");
        }

        public async Task<Tuple<ResponseInfo, string>> UploadAsync(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, byte[]> data = await UploadRawAsync(url, body, method, headers).ConfigureAwait(false);
            return new Tuple<ResponseInfo, string>(data.Item1, data.Item2.Length > 0 ? _encoding.GetString(data.Item2) : "{}");
        }

        public Tuple<ResponseInfo, byte[]> UploadRaw(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                AddHeaders(headers);
            }

            HttpRequestMessage message = new(new HttpMethod(method), url)
            {
                Content = new StringContent(body, _encoding)
            };
            using HttpResponseMessage response = Task.Run(() => _client.SendAsync(message)).Result;
            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
            {
                StatusCode = response.StatusCode,
                Headers = ConvertHeaders(response.Headers)
            }, Task.Run(() => response.Content.ReadAsByteArrayAsync()).Result);
        }

        public async Task<Tuple<ResponseInfo, byte[]>> UploadRawAsync(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                AddHeaders(headers);
            }

            HttpRequestMessage message = new(new HttpMethod(method), url)
            {
                Content = new StringContent(body, _encoding)
            };
            using HttpResponseMessage response = await _client.SendAsync(message);
            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
            {
                StatusCode = response.StatusCode,
                Headers = ConvertHeaders(response.Headers)
            }, await response.Content.ReadAsByteArrayAsync());
        }

        private bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}")) ||
                   (input.StartsWith("[") && input.EndsWith("]"));
        }

        public Tuple<ResponseInfo, T> UploadJson<T>(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, string> response = Upload(url, body, method, headers);
            try
            {
                if (IsJson(response.Item2))
                {
                    // If it's valid JSON, deserialize as usual.
                    T result = JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings);
                    return new Tuple<ResponseInfo, T>(response.Item1, result);
                }
                else
                {
                    // Non-JSON response. Spotify returns a plain string sometimes.
                    // If the status code is OK we assume it's a valid, non-error response.
                    // If not, you might consider assigning the text to an error message if T supports it.
                    T result = Activator.CreateInstance<T>();

                    // Optional: if you have an error property on T and the status code indicates a problem,
                    // you can assign the plain text as an error message.
                    if (response.Item1.StatusCode != HttpStatusCode.OK && result is ErrorResponse errorResponse)
                    {
                        errorResponse.Error = new Error { Message = response.Item2 };
                    }

                    return new Tuple<ResponseInfo, T>(response.Item1, result);
                }
            }
            catch (JsonException error)
            {
                // If deserialization fails, create a fallback error instance.
                string errorJson = string.Format(UnknownErrorJson, error.Message);
                T fallbackResult = JsonConvert.DeserializeObject<T>(errorJson, JsonSettings);
                return new Tuple<ResponseInfo, T>(response.Item1, fallbackResult);
            }
        }

        public async Task<Tuple<ResponseInfo, T>> UploadJsonAsync<T>(string url, string body, string method, Dictionary<string, string> headers = null)
        {
            Tuple<ResponseInfo, string> response = await UploadAsync(url, body, method, headers).ConfigureAwait(false);
            try
            {
                if (IsJson(response.Item2))
                {
                    // If it's valid JSON, deserialize as usual.
                    T result = JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings);
                    return new Tuple<ResponseInfo, T>(response.Item1, result);
                }
                else
                {
                    // Non-JSON response. Spotify returns a plain string sometimes.
                    // If the status code is OK we assume it's a valid, non-error response.
                    // If not, you might consider assigning the text to an error message if T supports it.
                    T result = Activator.CreateInstance<T>();

                    // Optional: if you have an error property on T and the status code indicates a problem,
                    // you can assign the plain text as an error message.
                    if (response.Item1.StatusCode != HttpStatusCode.OK && result is ErrorResponse errorResponse)
                    {
                        errorResponse.Error = new Error { Message = response.Item2 };
                    }

                    return new Tuple<ResponseInfo, T>(response.Item1, result);
                }
            }
            catch (JsonException error)
            {
                // If deserialization fails, create a fallback error instance.
                string errorJson = string.Format(UnknownErrorJson, error.Message);
                T fallbackResult = JsonConvert.DeserializeObject<T>(errorJson, JsonSettings);
                return new Tuple<ResponseInfo, T>(response.Item1, fallbackResult);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        private static WebHeaderCollection ConvertHeaders(HttpResponseHeaders headers)
        {
            WebHeaderCollection newHeaders = [];
            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in headers)
            {
                foreach (string headerValue in headerPair.Value)
                {
                    newHeaders.Add(headerPair.Key, headerValue);
                }
            }
            return newHeaders;
        }

        private void AddHeaders(Dictionary<string, string> headers)
        {
            _client.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, string> headerPair in headers)
            {
                _client.DefaultRequestHeaders.TryAddWithoutValidation(headerPair.Key, headerPair.Value);
            }
        }
    }
}