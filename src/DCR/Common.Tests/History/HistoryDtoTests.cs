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
            var testDelegate = new TestDelegate(() => new ActionDto());

            Assert.DoesNotThrow(testDelegate);
        }
    }
}
