namespace server.Core.Entities
{
    public class BusOperator : User
    {
        public string CompanyName { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public string Address { get; set; } = string.Empty;
    }
}
