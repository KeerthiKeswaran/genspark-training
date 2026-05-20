using System.Collections.Generic;
using System.Threading.Tasks;
using LibrarySystem.Models.DTOs;

namespace LibrarySystem.Contracts.Interfaces;

public interface IBookService
{
    Task<BookResponse> AddBook(BookRequest bookRequest);
    Task<IEnumerable<BookResponse>> GetAllBooks();
    Task<BookResponse> GetBookById(int id);
    Task<IEnumerable<BookResponse>> SearchBooksByTitle(string title);
}
