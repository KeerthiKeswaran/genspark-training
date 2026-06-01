namespace server.Core.Entities
{
    public class Admin : User
    {
        public bool IsSuperAdmin { get; set; } = false;
        public Guid? CreatedByAdminId { get; set; }
        public string Department { get; set; } = string.Empty;
    }
}
