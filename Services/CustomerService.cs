using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerListViewModel> GetPagedAsync(string? search, string sortBy, string sortDir, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

            var query = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c => c.FullName.Contains(term) || c.Email.Contains(term));
            }

            query = sortBy switch
            {
                "email" => sortDir == "desc" ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
                "date" => sortDir == "desc" ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                "orders" => sortDir == "desc" ? query.OrderByDescending(c => c.Orders.Count) : query.OrderBy(c => c.Orders.Count),
                _ => sortDir == "desc" ? query.OrderByDescending(c => c.FullName) : query.OrderBy(c => c.FullName)
            };

            var total = await query.CountAsync();
            var pageData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    Customer = c,
                    TotalOrders = c.Orders.Count,
                    TotalSpent = c.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0,
                    LastOrderDate = c.Orders.Select(o => (DateTime?)o.OrderDate).Max()
                })
                .ToListAsync();

            var items = pageData.Select(row => new CustomerViewModel
            {
                Id = row.Customer.Id,
                FullName = row.Customer.FullName,
                Email = row.Customer.Email,
                Phone = row.Customer.Phone,
                Address = row.Customer.Address,
                ProfileImagePath = row.Customer.ProfileImagePath,
                IsActive = row.Customer.IsActive,
                CreatedAt = row.Customer.CreatedAt,
                TotalOrders = row.TotalOrders,
                TotalSpent = row.TotalSpent,
                LastOrderDate = row.LastOrderDate?.ToString("dd MMM yyyy")
            }).ToList();

            return new CustomerListViewModel
            {
                Customers = new PaginatedList<CustomerViewModel>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                },
                SearchTerm = search,
                SortBy = sortBy,
                SortDir = sortDir,
                TotalCustomers = total
            };
        }

        public async Task<CustomerViewModel?> GetByIdAsync(int id)
        {
            var customer = await _context.Customers.AsNoTracking()
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            return new CustomerViewModel
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                ProfileImagePath = customer.ProfileImagePath,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt,
                TotalOrders = customer.Orders.Count,
                TotalSpent = customer.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = customer.Orders.Any()
                    ? customer.Orders.Max(o => o.OrderDate).ToString("dd MMM yyyy")
                    : null
            };
        }

        public async Task<bool> CreateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            var email = model.Email.Trim();

            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email.ToLower()))
                return false;

            var customer = new Customer
            {
                FullName = model.FullName.Trim(),
                Email = email,
                Phone = model.Phone?.Trim(),
                Address = model.Address?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            if (model.ProfileImage != null)
            {
                var (path, error) = await FileStorage.SaveImageAsync(model.ProfileImage, env, "customers");
                if (path == null) return false;
                customer.ProfileImagePath = path;
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            var customer = await _context.Customers.FindAsync(model.Id);
            if (customer == null) return false;

            var email = model.Email.Trim();
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email.ToLower() && c.Id != model.Id))
                return false;

            customer.FullName = model.FullName.Trim();
            customer.Email = email;
            customer.Phone = model.Phone?.Trim();
            customer.Address = model.Address?.Trim();
            customer.IsActive = model.IsActive;

            if (model.ProfileImage != null)
            {
                var (path, error) = await FileStorage.SaveImageAsync(model.ProfileImage, env, "customers");
                if (path == null) return false;

                FileStorage.DeleteImage(env, customer.ProfileImagePath);
                customer.ProfileImagePath = path;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, IWebHostEnvironment env)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return false;
            if (customer.Orders.Count != 0) return false;

            FileStorage.DeleteImage(env, customer.ProfileImagePath);

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetOrderHistoryAsync(int customerId)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
