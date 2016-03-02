using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Event
{
    public class WorkflowDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Id { get; set; }
    }
}
