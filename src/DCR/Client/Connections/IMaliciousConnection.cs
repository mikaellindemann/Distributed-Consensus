using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Connections
{
    public interface IMaliciousConnection
    {
        Task HistoryAboutOthers(Uri uri, string workflowId, string eventId);
        Task MixUpLocalTimestamp(Uri uri, string workflowId, string eventId);
    }
}
