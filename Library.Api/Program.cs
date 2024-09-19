using FluentValidation;
using FluentValidation.Results;
using Library.Api.Data;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

//services
//

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// add IDbConnectionFactory injection
builder.Services.AddSingleton<IDbConnectionFactory>(_ => 
    new SqliteConnectionFactory(builder.Configuration.GetValue<string>("Database:ConnectionString")));

// add DatabaseInitializer service
builder.Services.AddSingleton<DatabaseInitializer>();

// add my custom services
builder.Services.AddSingleton<IBookService, BookService>();

// add all Fluent Validators found in the current assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

//middleware
//

// use swagger
app.UseSwagger();
app.UseSwaggerUI();

// use routes
app.MapGet("/", () => "Hello World!");

app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
{
    if (!string.IsNullOrEmpty(searchTerm))
    {
        var book = await bookService.SearchByTitleAsync(searchTerm);
        return Results.Ok(book);
    }

    var books = await bookService.GetAllAsync();
    return Results.Ok(books);
});

app.MapGet("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var book = await bookService.GetByIsbnAsync(isbn);

    return book is not null ? Results.Ok(book) : Results.NotFound();
});

app.MapPost("books", async (Book book, IBookService bookService, IValidator<Book> validator) => 
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

    return Results.Created($"/books/{book.Isbn}", book);
});

app.MapPut("books/{isbn}", async (string isbn, Book book, IBookService bookService, IValidator<Book> validator) =>
{
    book.Isbn = isbn;

    var validationResult = await validator.ValidateAsync(book);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var updated = await bookService.UpdateAsync(book);

    return updated ? Results.Ok(book) : Results.NotFound();
});

app.MapDelete("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var deleted = await bookService.DeleteAsync(isbn);

    return deleted ? Results.NoContent() : Results.NotFound();
});



// use DatabaseInitializer service to create SQLite database locally
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
