namespace server.Core.Enums
{
    public enum BookingStatus
    {
        Confirmed,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }

    public enum Gender
    {
        M,
        F,
        Other
    }

    public enum JourneyStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }
}
