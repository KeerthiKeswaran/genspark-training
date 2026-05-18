using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;

namespace LibrarySystem.Business.Services;

public class BookService : IBookService
{
    private readonly ILibraryRepository _repository;

    public BookService(ILibraryRepository repository)
    {
        _repository = repository;
    }

    public void AddBook(string? title, string? author, string? categoryName, int initialCopies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new Exception("Title is required.");
        if (string.IsNullOrWhiteSpace(categoryName)) throw new Exception("Category is required.");

        // 1. Handle Category
        var existingCategory = _repository.GetAllCategories().FirstOrDefault(c => c.Categoryname == categoryName);
        int categoryId;

        if (existingCategory != null)
        {
            categoryId = existingCategory.Categorynumber;
        }
        else
        {
            // Create new category
            var newCategory = new Bookcategory { Categoryname = categoryName };
            _repository.AddCategory(newCategory);
            _repository.SaveChanges();
            categoryId = newCategory.Categorynumber;
        }

        // 2. Create Book
        var book = new Book
        {
            Booktitle = title ?? "",
            Bookauthor = author ?? "Unknown",
            Bookcategory = categoryName,
            Categorynumber = categoryId
        };

        _repository.AddBook(book);
        _repository.SaveChanges();

        if (initialCopies > 0)
        {
            AddCopies(book.Bookid, initialCopies);
        }
    }

    public void AddCopies(int bookId, int count)
    {
        if (count <= 0) throw new Exception("Count must be greater than zero.");
        
        for (int i = 0; i < count; i++)
        {
            _repository.AddBookCopy(new Bookcopy
            {
                Bookid = bookId,
                Copystatus = "Available",
                Copycondition = "Good"
            });
        }
        _repository.SaveChanges();
    }

    public Book? GetBookById(int id) => _repository.GetBookById(id);
    public int GetCopyCount(int bookId) => _repository.GetCopiesByBookId(bookId).Count();

    public IEnumerable<Book> GetBooks() => _repository.GetAllBooks();

    public IEnumerable<Bookcategory> GetCategories() => _repository.GetAllCategories();

    public IEnumerable<Book> SearchBooks(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString)) return GetBooks();
        
        return _repository.GetAllBooks().Where(b => 
            b.Booktitle.Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
            b.Bookauthor.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
            (b.CategorynumberNavigation != null && b.CategorynumberNavigation.Categoryname.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
    }

    public void UpdateCopyCondition(int copyId, string newCondition)
    {
        var copy = _repository.GetBookCopyById(copyId);
        if (copy == null) throw new Exception("Book copy not found.");
        
        copy.Copycondition = newCondition;
        _repository.UpdateBookCopy(copy);
        _repository.SaveChanges();
    }
}
