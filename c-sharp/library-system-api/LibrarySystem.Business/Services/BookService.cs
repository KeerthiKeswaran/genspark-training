using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibrarySystem.Business.Exceptions;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Models.DTOs;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Business.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookResponse> AddBook(BookRequest bookRequest)
    {
        if (string.IsNullOrWhiteSpace(bookRequest.Title))
        {
            throw new ValidationException("Book title should not be empty.");
        }

        if (string.IsNullOrWhiteSpace(bookRequest.Author))
        {
            throw new ValidationException("Author name should not be empty.");
        }

        if (bookRequest.AvailableCopies < 0)
        {
            throw new ValidationException("Available copies should be greater than or equal to 0.");
        }

        var book = new Book
        {
            Title = bookRequest.Title.Trim(),
            Author = bookRequest.Author.Trim(),
            ISBN = bookRequest.ISBN?.Trim() ?? string.Empty,
            PublishedYear = bookRequest.PublishedYear,
            AvailableCopies = bookRequest.AvailableCopies
        };

        await _bookRepository.AddBook(book);

        return new BookResponse
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            PublishedYear = book.PublishedYear,
            AvailableCopies = book.AvailableCopies
        };
    }

    public async Task<IEnumerable<BookResponse>> GetAllBooks()
    {
        var books = await _bookRepository.GetAllBooks();
        return books.Select(b => new BookResponse
        {
            BookId = b.BookId,
            Title = b.Title,
            Author = b.Author,
            ISBN = b.ISBN,
            PublishedYear = b.PublishedYear,
            AvailableCopies = b.AvailableCopies
        });
    }

    public async Task<BookResponse> GetBookById(int id)
    {
        var book = await _bookRepository.GetBookById(id);
        if (book == null)
        {
            throw new NotFoundException($"Book with ID {id} was not found.");
        }

        return new BookResponse
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            PublishedYear = book.PublishedYear,
            AvailableCopies = book.AvailableCopies
        };
    }

    public async Task<IEnumerable<BookResponse>> SearchBooksByTitle(string title)
    {
        var books = await _bookRepository.SearchBooksByTitle(title);
        return books.Select(b => new BookResponse
        {
            BookId = b.BookId,
            Title = b.Title,
            Author = b.Author,
            ISBN = b.ISBN,
            PublishedYear = b.PublishedYear,
            AvailableCopies = b.AvailableCopies
        });
    }
}
