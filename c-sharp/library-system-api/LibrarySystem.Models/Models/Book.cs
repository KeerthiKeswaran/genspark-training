namespace LibrarySystem.Models.Models;

public class Book
{
    public int BookId { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string ISBN { get; set; }
    public int PublishedYear { get; set; }
    public int AvailableCopies { get; set; }
}
