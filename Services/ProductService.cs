using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== GET PAGED =====
        public async Task<ProductViewModel> GetPagedAsync(string? search, int? categoryId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.Description.Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    DiscountPercent = p.DiscountPercent,
                    StockQuantity = p.StockQuantity,
                    LowStockThreshold = p.LowStockThreshold,
                    Barcode = p.Barcode,
                    ProductCode = p.ProductCode,
                    Status = p.Status,
                    ImagePath = p.ImagePath,
                    ExistingImages = string.IsNullOrEmpty(p.ImagePath)
                        ? new List<string>()
                        : new List<string> { p.ImagePath }
                })
                .ToListAsync();

            return new ProductViewModel
            {
                Products = new PaginatedList<ProductViewModel>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                },
                SearchTerm = search,
                CategoryFilter = categoryId
            };
        }

        // ===== GET BY ID =====
        public async Task<ProductViewModel?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return null;
            }

            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                Barcode = product.Barcode,
                ProductCode = product.ProductCode,
                Status = product.Status,
                ImagePath = product.ImagePath,
                ExistingImages = string.IsNullOrEmpty(product.ImagePath)
                    ? new List<string>()
                    : new List<string> { product.ImagePath }
            };
        }

        // ===== GET LAST PRODUCT =====
        public async Task<Product?> GetLastProductAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        // ===== CREATE =====
        public async Task CreateAsync(ProductViewModel model, IWebHostEnvironment env)
        {
            var barcode = await BarcodeHelper.GenerateUniqueBarcodeAsync(
                code => _context.Products.AnyAsync(p => p.Barcode == code));

            var product = new Product
            {
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                CategoryId = model.CategoryId,
                Price = model.Price,
                DiscountPercent = model.DiscountPercent,
                StockQuantity = model.StockQuantity,
                LowStockThreshold = model.LowStockThreshold,
                Barcode = barcode,
                ProductCode = model.ProductCode,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow
            };

            var image = model.NewImages?.FirstOrDefault();
            if (image != null)
            {
                product.ImagePath = await FileUploadHelper.SaveImageAsync(image, env, "images/products");
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        // ===== UPDATE =====
        public async Task<bool> UpdateAsync(ProductViewModel model, IWebHostEnvironment env)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (product == null)
            {
                return false;
            }

            if (model.CategoryId > 0 &&
                !await _context.Categories.AnyAsync(c => c.Id == model.CategoryId))
            {
                return false;
            }

            product.Name = model.Name.Trim();
            product.Description = model.Description.Trim();
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.DiscountPercent = model.DiscountPercent;
            product.StockQuantity = model.StockQuantity;
            product.LowStockThreshold = model.LowStockThreshold;
            product.Barcode = model.Barcode;
            product.ProductCode = model.ProductCode;
            product.Status = model.Status;

            var image = model.NewImages?.FirstOrDefault();
            if (image != null)
            {
                var newPath = await FileUploadHelper.SaveImageAsync(image, env, "images/products");
                FileUploadHelper.DeleteIfExists(env, product.ImagePath);
                product.ImagePath = newPath;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ===== DELETE =====
        public async Task DeleteAsync(int id, IWebHostEnvironment env)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return;
            }

            // Keep historic order lines intact: only remove products that were never ordered.
            if (await _context.OrderItems.AnyAsync(i => i.ProductId == id))
            {
                product.Status = ProductStatus.Discontinued;
                await _context.SaveChangesAsync();
                return;
            }

            FileUploadHelper.DeleteIfExists(env, product.ImagePath);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        // ===== GET CATEGORIES =====
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
