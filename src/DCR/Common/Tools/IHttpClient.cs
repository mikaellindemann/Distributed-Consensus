using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Common.Tools
{
    public interface IHttpClient : IDisposable
    {
        HttpRequestHeaders DefaultRequestHeaders { get; }
        Uri BaseAddress { get; set; }
        TimeSpan Timeout { get; set; }
        Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T obj);
        Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T obj);
        Task<HttpResponseMessage> DeleteAsync(string uri);
        Task<HttpResponseMessage> GetAsync(string uri);
    }
}