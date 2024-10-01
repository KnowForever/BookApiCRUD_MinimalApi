using FluentValidation.Results;
using FluentValidation;
using Library.Api.Endpoints.Internal;
using Library.Api.Models;
using Library.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace Library.Api.Endpoints
{
    public class LibraryEndpoints : IEndpoint
    {
        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IBookService, BookService>();
        }

        public static void DefineEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("books", GetAllBooksAsync)
                .WithName("GetBooks")
                .Produces<IEnumerable<Book>>(200)
                .WithTags("Books");

            app.MapGet("books/{isbn}", GetBookByIsbnAsync)
                .WithName("GetBook")
                .Produces<Book>(200)
                .Produces(404)
                .WithTags("Books");

            app.MapPost("books", CreateBookAsync)
                .WithName("CreateBook")
                .Accepts<Book>("application/json")
                .Produces<Book>(201)
                .Produces<IEnumerable<ValidationFailure>>(400)
                .WithTags("Books");

            app.MapPut("books/{isbn}", UpdateBookAsync)
                .WithName("UpdateBook")
                .Accepts<Book>("application/json")
                .Produces<Book>(200)
                .Produces<IEnumerable<ValidationFailure>>(400)
                .Produces(404)
                .WithTags("Books");

            app.MapDelete("books/{isbn}", DeleteBookAsync)
                .WithName("DeleteBook")
                .Produces(204)
                .Produces(404)
                .WithTags("Books");
        }

        #region Endpoints...
        internal static async Task<IResult> GetAllBooksAsync(IBookService bookService, string? searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var book = await bookService.SearchByTitleAsync(searchTerm);
                return Results.Ok(book);
            }

            var books = await bookService.GetAllAsync();
            return Results.Ok(books);
        }

        internal static async Task<IResult> GetBookByIsbnAsync(string isbn, IBookService bookService)
        {
            var book = await bookService.GetByIsbnAsync(isbn);
            return book is not null ? Results.Ok(book) : Results.NotFound();
        }

        internal static async Task<IResult> CreateBookAsync(Book book, IBookService bookService, IValidator<Book> validator)
        {
            var validationResult = await validator.ValidateAsync(book);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var created = await bookService.CreateAsync(book);

            if (!created)
            {
                return Results.BadRequest(new List<ValidationFailure>
                {
                    new ValidationFailure("Isbn", "A book with this ISBN-13 already exists")
                });
            }

            //return Results.Created($"/books/{book.Isbn}", book);
            return Results.CreatedAtRoute("GetBook", new { isbn = book.Isbn }, book);
        }

        internal static async Task<IResult> UpdateBookAsync(string isbn, Book book, IBookService bookService, IValidator<Book> validator)
        {
            book.Isbn = isbn;

            var validationResult = await validator.ValidateAsync(book);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var updated = await bookService.UpdateAsync(book);
            return updated ? Results.Ok(book) : Results.NotFound();
        }

        internal static async Task<IResult> DeleteBookAsync(string isbn, IBookService bookService)
        {
            var deleted = await bookService.DeleteAsync(isbn);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        #endregion
    }
}
