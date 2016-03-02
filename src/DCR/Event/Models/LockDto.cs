namespace Event.Models
{
    public class LockDto
    {
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        //It's expected that LockOwner matches the EventId of the EventAddressDto making the lock call.
        public string LockOwner { get; set; }   
    }
}