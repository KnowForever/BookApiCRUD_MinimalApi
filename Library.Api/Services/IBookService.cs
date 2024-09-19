using Library.Api.Models;

namespace Library.Api.Services
{
    public interface IBookService
    {
        Task<bool> CreateAsync(Book book);

        Task<IEnumerable<Book>> GetAllAsync();

        Task<Book?> GetByIsbnAsync(string isbn);
        
        Task<IEnumerable<Book>> SearchByTitleAsync(string searchTerm);

        Task<bool> UpdateAsync(Book book);

        Task<bool> DeleteAsync(string isbn);


    }
}
