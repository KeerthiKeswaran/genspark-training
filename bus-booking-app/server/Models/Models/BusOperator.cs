using server.Core.Enums;

namespace server.Core.Entities
{
    public class BusOperator : User
    {
        public string CompanyName { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public string? RejectionReason { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}
