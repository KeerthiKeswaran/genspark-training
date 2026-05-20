using System.Collections.Generic;
using System.Threading.Tasks;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Contracts.Interfaces;

public interface IBookRepository
{
    Task AddBook(Book book);
    Task<IEnumerable<Book>> GetAllBooks();
    Task<Book?> GetBookById(int id);
    Task<IEnumerable<Book>> SearchBooksByTitle(string title);
}
