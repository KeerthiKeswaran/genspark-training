using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data.Contexts;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Data.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task AddBook(Book book)
    {
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Book>> GetAllBooks()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<Book?> GetBookById(int id)
    {
        return await _context.Books.FindAsync(id);
    }

    public async Task<IEnumerable<Book>> SearchBooksByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return await _context.Books.ToListAsync();
        }

        return await _context.Books
            .Where(b => EF.Functions.ILike(b.Title, $"%{title}%"))
            .ToListAsync();
    }
}
