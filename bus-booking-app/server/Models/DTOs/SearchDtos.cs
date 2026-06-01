namespace server.Features.Search
{
    public record SearchBusRequest(string From, string To, DateTime Date);
    
    public record BusSearchResult(
        Guid ScheduleId,
        string BusNumber,
        string BusType,
        string OperatorName,
        string OperatorAddress,
        string Source,
        string Destination,
        DateTime DepartureTime,
        DateTime ArrivalTime,
        decimal Price,
        int AvailableSeats
    );
}
