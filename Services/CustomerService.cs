using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
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
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(term) ||
                    c.Email.Contains(term) ||
                    (c.Phone != null && c.Phone.Contains(term)));
            }

            query = sortBy?.ToLowerInvariant() switch
            {
                "email" => sortDir == "asc" ? query.OrderBy(c => c.Email) : query.OrderByDescending(c => c.Email),
                "date" => sortDir == "asc" ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
                _ => sortDir == "asc" ? query.OrderBy(c => c.FullName) : query.OrderByDescending(c => c.FullName)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerViewModel
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    ProfileImagePath = c.ProfileImagePath,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    TotalOrders = c.Orders.Count,
                    TotalSpent = c.Orders
                        .Where(o => o.OrderStatus != "Cancelled" && o.OrderStatus != "Refunded")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .ToListAsync();

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
                SortBy = sortBy ?? "name",
                SortDir = sortDir,
                TotalCustomers = total
            };
        }

        public async Task<CustomerViewModel?> GetByIdAsync(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return null;
            }

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
                TotalSpent = customer.Orders
                    .Where(o => o.OrderStatus != "Cancelled" && o.OrderStatus != "Refunded")
                    .Sum(o => o.TotalAmount)
            };
        }

        public async Task<bool> CreateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            var email = model.Email.Trim();

            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email.ToLower()))
            {
                return false;
            }

            var customer = new Customer
            {
                FullName = model.FullName.Trim(),
                Email = email,
                Phone = model.Phone?.Trim(),
                Address = model.Address?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            if (model.ProfileImage != null)
            {
                customer.ProfileImagePath = await FileUploadHelper.SaveImageAsync(model.ProfileImage, env, "images/customers");
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (customer == null)
            {
                return false;
            }

            var email = model.Email.Trim();
            if (await _context.Customers.AnyAsync(c => c.Id != model.Id && c.Email.ToLower() == email.ToLower()))
            {
                return false;
            }

            customer.FullName = model.FullName.Trim();
            customer.Email = email;
            customer.Phone = model.Phone?.Trim();
            customer.Address = model.Address?.Trim();
            customer.IsActive = model.IsActive;

            if (model.ProfileImage != null)
            {
                var newPath = await FileUploadHelper.SaveImageAsync(model.ProfileImage, env, "images/customers");
                FileUploadHelper.DeleteIfExists(env, customer.ProfileImagePath);
                customer.ProfileImagePath = newPath;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, IWebHostEnvironment env)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null || customer.Orders.Count > 0)
            {
                return false;
            }

            FileUploadHelper.DeleteIfExists(env, customer.ProfileImagePath);
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetOrderHistoryAsync(int customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
