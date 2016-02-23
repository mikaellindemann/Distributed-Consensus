namespace Common.DTO.Event
{
    public class EventStateDto
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Pending { get; set; }
        public bool Executed { get; set; }
        public bool Included { get; set; }
        public bool Executable { get; set; }
    }
}
