using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Event.Interfaces;
using Event.Logic;

namespace Event.Controllers
{
    public class MaliciousController : ApiController
    {
        private readonly IMaliciousLogic _maliciousLogic;

        public MaliciousController(IMaliciousLogic logic)
        {
            _maliciousLogic = logic;
        }

        [Route("event/malicious/{workflowId}/{eventId}/HistoryAboutOthers")]
        [HttpPut]
        public async Task HistoryAboutOthers(string workflowId, string eventId)
        {
            await _maliciousLogic.HistoryAboutOthers(workflowId, eventId);
        }

        [Route("event/malicious/{workflowId}/{eventId}/MixUpLocalTimestamp")]
        [HttpPut]
        public async Task MixUpLocalTimestamp(string workflowId, string eventId)
        {
            await MixUpLocalTimestamp(workflowId,eventId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _maliciousLogic.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
