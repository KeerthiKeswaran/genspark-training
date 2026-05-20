using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Models.DTOs;
using LibrarySystem.Business.Exceptions;

namespace LibrarySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpPost]
    public async Task<IActionResult> AddBook([FromBody] BookRequest bookRequest)
    {
        try
        {
            var result = await _bookService.AddBook(bookRequest);
            return Ok(new { message = "Book added successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBooks()
    {
        var result = await _bookService.GetAllBooks();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        try
        {
            var result = await _bookService.GetBookById(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks([FromQuery] string title)
    {
        var result = await _bookService.SearchBooksByTitle(title);
        return Ok(result);
    }
}
