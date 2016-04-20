using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Client.Exceptions;
using Common.Tools;

namespace Client.Connections
{
    public class MaliciousConnection : IMaliciousConnection
    {
        private readonly HttpClientToolbox _httpClient;

        /// <summary>
        /// This constructor is used forwhen the connection should have knowlegde about roles.
        /// </summary>
        public MaliciousConnection()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromHours(1) };
            _httpClient = new HttpClientToolbox(new HttpClientWrapper(client));
        }
        /// <summary>
        /// For testing purposes (dependency injection of mocked Toolbox).
        /// </summary>
        /// <param name="toolbox"></param>
        public MaliciousConnection(HttpClientToolbox toolbox)
        {
            _httpClient = toolbox;
        }

        public async Task HistoryAboutOthers(Uri uri, string workflowId, string eventId)
        {
            try
            {
                await _httpClient.Update($"{uri}event/malicious/{workflowId}/{eventId}/HistoryAboutOthers", (object)null);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public async Task MixUpLocalTimestamp(Uri uri, string workflowId, string eventId)
        {
            try
            {
                await _httpClient.Update($"{uri}event/malicious/{workflowId}/{eventId}/MixUpLocalTimestamp", (object)null);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }
    }
}
