using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Common.Tools
{
    public class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        public Uri BaseAddress
        {
            get { return _httpClient.BaseAddress; }
            set { _httpClient.BaseAddress = value; }
        }

        public TimeSpan Timeout
        {
            get { return _httpClient.Timeout; }
            set { _httpClient.Timeout = value; }
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T obj)
        {
            return await _httpClient.PostAsJsonAsync(uri, obj);
        }

        public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T obj)
        {
            return await _httpClient.PutAsJsonAsync(uri, obj);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string uri)
        {
            return await _httpClient.DeleteAsync(uri);
        }

        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            return await _httpClient.GetAsync(uri);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
