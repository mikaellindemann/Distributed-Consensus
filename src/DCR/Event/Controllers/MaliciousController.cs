using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
