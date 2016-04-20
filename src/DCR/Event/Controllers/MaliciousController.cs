using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Shared;
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

        [Route("event/malicious/{workflowId}/{eventId}")]
        [HttpPut]
        public async Task ApplyCheatingType(string workflowId, string eventId, [FromBody] CheatingDto cheatingDto)
        {
            await _maliciousLogic.ApplyCheatingType(workflowId, eventId, cheatingDto);
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
