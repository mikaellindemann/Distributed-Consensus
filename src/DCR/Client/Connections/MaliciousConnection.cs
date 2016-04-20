using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Client.Exceptions;
using Common;
using Common.DTO.Shared;
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

        public async Task ApplyCheatingType(Uri uri, string workflowId, string eventId, CheatingTypeEnum cheatingType)
        {
            try
            {
                var cheatingDto = new CheatingDto {CheatingTypeEnum = cheatingType};
                await _httpClient.Update($"{uri}event/malicious/{workflowId}/{eventId}", cheatingDto);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
