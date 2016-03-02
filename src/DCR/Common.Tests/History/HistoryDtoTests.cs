using Common.DTO.History;
using NUnit.Framework;

namespace Common.Tests.History
{
    [TestFixture]
    public class HistoryDtoTests
    {
        [Test]
        public void HistoryDto_NoArguments()
        {
            var testDelegate = new TestDelegate(() => new HistoryDto());

            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void HistoryDto_Takes_HistoryModel_As_Argument()
        {
            var historymodel = new HistoryModel();

            var testDelegate = new TestDelegate(() => new HistoryDto(historymodel));

            Assert.DoesNotThrow(testDelegate);
        }
    }
}
