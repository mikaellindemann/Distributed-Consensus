using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum CheatingTypeEnum
    {
        HistoryAboutOthers,
        FakeRelationsOut,
        FakeRelationsIn,
        LocalTimestampOutOfOrder,
        IncomingChangesWhileExecuting,
        PartialOutgoingWhenExecuting,
        CounterpartTimestampOutOfOrder
    }
}
