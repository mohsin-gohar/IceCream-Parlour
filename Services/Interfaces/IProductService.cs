using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public interface IProductService
    {
        Task<ProductViewModel> GetPagedAsync(string? search, int? categoryId, int page, int pageSize);
        Task<ProductViewModel?> GetByIdAsync(int id);
        Task<Product?> GetLastProductAsync();   
        Task CreateAsync(ProductViewModel model, IWebHostEnvironment env);
        Task<bool> UpdateAsync(ProductViewModel model, IWebHostEnvironment env);
        Task DeleteAsync(int id, IWebHostEnvironment env);
        Task<List<Category>> GetCategoriesAsync();
    }
}