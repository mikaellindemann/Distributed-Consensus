using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Client.Connections
{
    public interface IMaliciousConnection
    {
        Task ApplyCheatingType(Uri uri, string workflowId, string eventId, CheatingTypeEnum cheatingType);
    }
}
