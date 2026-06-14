namespace Event.Models.DTOs
{
    public class StaffResponse
    {
        public int Employee_ID { get; set; }
        public string Region_Id { get; set; } = string.Empty;
        public bool IsAllocated { get; set; }
    }
}
