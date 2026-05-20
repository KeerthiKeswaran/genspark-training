namespace LibrarySystem.Models.DTOs;

public class BookRequest
{
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string ISBN { get; set; }
    public int PublishedYear { get; set; }
    public int AvailableCopies { get; set; }
}
