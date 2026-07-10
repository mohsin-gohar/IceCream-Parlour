using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
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
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    CategoryName = _context.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    Price = p.Price,
                    DiscountPercent = p.DiscountPercent,
                    StockQuantity = p.StockQuantity,
                    LowStockThreshold = p.LowStockThreshold,
                    Barcode = p.Barcode,
                    ProductCode = p.ProductCode,
                    Status = (ProductStatus)p.Status,
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
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                Barcode = product.Barcode,
                ProductCode = product.ProductCode,
                Status = (ProductStatus)product.Status,
                ExistingImages = string.IsNullOrEmpty(product.ImagePath)
                    ? new List<string>()
                    : new List<string> { product.ImagePath }
            };
        }

        // ===== GET LAST PRODUCT =====
        public async Task<Product?> GetLastProductAsync()
        {
            return await _context.Products
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        // ===== CREATE =====
        public async Task CreateAsync(ProductViewModel model, IWebHostEnvironment env)
        {
            // ✅ Generate unique 12-digit barcode
            string barcode;
            var rand = new Random();
            do
            {
                barcode = rand.Next(100000000, 999999999).ToString("D9") +
                          rand.Next(1000, 9999).ToString("D4");
            } while (await _context.Products.AnyAsync(p => p.Barcode == barcode));

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Price = model.Price,
                DiscountPercent = model.DiscountPercent,
                StockQuantity = model.StockQuantity,
                LowStockThreshold = model.LowStockThreshold,
                Barcode = barcode,
                ProductCode = model.ProductCode,   // ✅ Save ProductCode
                Status = (ProductStatus)model.Status,
                CreatedAt = DateTime.Now
            };

            // ✅ Image upload
            if (model.NewImages != null && model.NewImages.Any())
            {
                var folder = "images/products/";
                var fileName = Guid.NewGuid().ToString() + "_" + model.NewImages.First().FileName;
                var fullPath = Path.Combine(env.WebRootPath, folder, fileName);
                Directory.CreateDirectory(Path.Combine(env.WebRootPath, folder));
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.NewImages.First().CopyToAsync(stream);
                product.ImagePath = "/" + folder + fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        // ===== UPDATE =====
        public async Task<bool> UpdateAsync(ProductViewModel model, IWebHostEnvironment env)
        {
            var product = await _context.Products.FindAsync(model.Id);
            if (product == null) return false;

            product.Name = model.Name;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.DiscountPercent = model.DiscountPercent;
            product.StockQuantity = model.StockQuantity;
            product.LowStockThreshold = model.LowStockThreshold;
            product.Barcode = model.Barcode;
            product.ProductCode = model.ProductCode;
            product.Status = (ProductStatus)model.Status;

            // Image upload for update
            if (model.NewImages != null && model.NewImages.Any())
            {
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    var oldPath = Path.Combine(env.WebRootPath, product.ImagePath.TrimStart('/'));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                var folder = "images/products/";
                var fileName = Guid.NewGuid().ToString() + "_" + model.NewImages.First().FileName;
                var fullPath = Path.Combine(env.WebRootPath, folder, fileName);
                Directory.CreateDirectory(Path.Combine(env.WebRootPath, folder));
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.NewImages.First().CopyToAsync(stream);
                product.ImagePath = "/" + folder + fileName;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ===== DELETE =====
        public async Task DeleteAsync(int id, IWebHostEnvironment env)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    var oldPath = Path.Combine(env.WebRootPath, product.ImagePath.TrimStart('/'));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        // ===== GET CATEGORIES =====
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }
    }
}