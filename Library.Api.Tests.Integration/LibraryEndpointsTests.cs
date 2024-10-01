using FluentAssertions;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Library.Api.Tests.Integration
{
    public class LibraryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly List<string> _createdIsbns = new List<string>();

        public LibraryEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            var httpClient = _factory.CreateClient();

            foreach (var createdIsbn in _createdIsbns)
            {
                await httpClient.DeleteAsync($"/books/{createdIsbn}");
            }
        }

        [Fact]
        public async Task CreateBook_CreatesBook_WhenDataIsCorrect()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            //Act
            var result = await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var createdBook = await result.Content.ReadFromJsonAsync<Book>();


            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.Created);
            createdBook.Should().BeEquivalentTo(book);
            result.Headers.Location.Should().NotBeNull();
            result.Headers.Location!.AbsolutePath.Should().Be($"/books/{book.Isbn}");
        }

        [Fact]
        public async Task CreateBook_Fails_WhenIsbnIsInvalid()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            book.Isbn = "INVALID_BLEH";

            //Act
            var result = await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Isbn");
            error.ErrorMessage.Should().Be("Value was not a valid ISBN-13");
        }

        [Fact]
        public async Task CreateBook_Fails_WhenBookExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            //Act
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var result = await httpClient.PostAsJsonAsync("/books", book); //create same book again
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Isbn");
            error.ErrorMessage.Should().Be("A book with this ISBN-13 already exists");
        }

        [Fact]
        public async Task GetBook_ReturnsBook_WhenBookExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            //Act
            var result = await httpClient.GetAsync($"/books/{book.Isbn}");
            var existingBook = await result.Content.ReadFromJsonAsync<Book>();

            //Assert
            existingBook.Should().BeEquivalentTo(book);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetBook_ReturnsNotFound_WhenBookDoesNotExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var isbn = GenerateIsbn();

            //Act
            var result = await httpClient.GetAsync($"/books/{isbn}");

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAllBooks_ReturnsAllBooks_WhenBooksExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var books = new List<Book> { book };

            //Act
            var result = await httpClient.GetAsync("/books");
            var allBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            allBooks.Should().BeEquivalentTo(books);
        }

        [Fact]
        public async Task GetAllBooks_ReturnsNoBooks_WhenNoBooksExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();

            //Act
            var result = await httpClient.GetAsync("/books");
            var allBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            allBooks.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchBooks_ReturnsBooks_WhenTitleMatches()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var books = new List<Book> { book };

            //Act
            var result = await httpClient.GetAsync("/books?searchTerm=oder");
            var allBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            allBooks.Should().BeEquivalentTo(books);
        }

        [Fact]
        public async Task UpdateBook_UpdatesBook_WhenDataIsCorrect()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            //Act
            book.PageCount = 69;
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
            var updatedBook = await result.Content.ReadFromJsonAsync<Book>();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            updatedBook.Should().BeEquivalentTo(book);
        }

        [Fact]
        public async Task UpdateBook_DoesNotUpdateBook_WhenDataIsNotCorrect()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            //Act
            book.Title = string.Empty;
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Title");
            error.ErrorMessage.Should().Be("'Title' must not be empty.");
        }

        [Fact]
        public async Task UpdateBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            //Act
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteBook_ReturnsNoContent_WhenBookExists()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            //Act
            var result = await httpClient.DeleteAsync($"/books/{book.Isbn}");

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            //Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            //Act
            var result = await httpClient.DeleteAsync($"/books/{book.Isbn}");

            //Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private Book GenerateBook(string title = "The Dirty Coder")
        {
            return new Book
            {
                Isbn = GenerateIsbn(),
                Title = title,
                Author = "Nick Chapsas",
                ShortDescription = "All my tricks in one book",
                PageCount = 420,
                ReleaseDate = new DateTime(2023, 1, 1)
            };
        }

        private string GenerateIsbn()
        {
            return $"{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000000000, 2100999999)}";
        }
    }
}
