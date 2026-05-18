using LibrarySystem.Data;

namespace LibrarySystem.Contracts.Interfaces;

public interface IBookService
{
    void AddBook(string? title, string? author, string? category, int initialCopies);
    Book? GetBookById(int id);
    int GetCopyCount(int bookId);
    void AddCopies(int bookId, int count);
    IEnumerable<Book> GetBooks();
    IEnumerable<Book> SearchBooks(string searchString);
    IEnumerable<Bookcategory> GetCategories();
    void UpdateCopyCondition(int copyId, string newCondition);
}
