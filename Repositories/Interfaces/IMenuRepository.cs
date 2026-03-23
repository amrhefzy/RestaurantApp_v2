using RestaurantMS.Models.Domain;

namespace RestaurantMS.Repositories.Interfaces;

public interface IMenuRepository : IGenericRepository<MenuItem>
{
    Task<IEnumerable<Category>> GetMenuWithCategoriesAsync();
    Task<MenuItem?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<MenuItem>> SearchMenuAsync(string term);
}
