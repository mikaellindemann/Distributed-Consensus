using System;

namespace Common.DTO.History
{
    public class HistoryModel
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string HttpRequestType { get; set; }
        public string MethodCalledOnSender { get; set; }
        public string Message { get; set; }

        public HistoryModel()
        {
            TimeStamp = DateTime.Now;
        }
    }
}
