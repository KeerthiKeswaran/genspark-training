using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    [Table("Staffs")]
    public class Staff
    {
        [Key]
        public int Employee_ID { get; set; }
        
        public string Region_Id { get; set; } = string.Empty;
        public virtual Region Region { get; set; } = null!;
        
        public bool IsAllocated { get; set; }

        // Navigation properties
        public virtual ICollection<EventStaffAllocation> EventAllocations { get; set; } = new List<EventStaffAllocation>();
    }
}
