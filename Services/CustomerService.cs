using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Services.Interfaces;
using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Helpers;

namespace Ice_Cream_Parlour_Eproject.Services
{
    /// <summary>
    /// Service for managing customer operations with error handling and logging.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerService> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomerService class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">The logger instance</param>
        /// <exception cref="ArgumentNullException">Thrown when context or logger is null</exception>
        public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a paginated list of customers with optional search and sorting.
        /// </summary>
        /// <param name="search">Optional search term</param>
        /// <param name="sortBy">The field to sort by</param>
        /// <param name="sortDir">Sort direction (asc/desc)</param>
        /// <param name="page">The page number</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>A paged list of customers</returns>
        public async Task<CustomerListViewModel> GetPagedAsync(string? search, string sortBy, string sortDir, int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("Getting paginated customers with search={Search}, sortBy={SortBy}, page={Page}", search, sortBy, page);

                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                var query = _context.Customers.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search));

                query = sortBy switch
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
                        TotalSpent = c.Orders.Sum(o => o.TotalAmount)
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} customers from {Total} total", items.Count, total);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged customers");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a customer by ID with their orders.
        /// </summary>
        /// <param name="id">The customer ID</param>
        /// <returns>The customer view model or null if not found</returns>
        public async Task<CustomerViewModel?> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid customer ID requested: {Id}", id);
                    return null;
                }

                var customer = await _context.Customers
                    .Include(c => c.Orders)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found with ID: {Id}", id);
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
                    TotalSpent = customer.Orders.Sum(o => o.TotalAmount)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer with ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Creates a new customer with profile image handling.
        /// </summary>
        /// <param name="model">The customer view model</param>
        /// <param name="env">The web host environment</param>
        /// <returns>True if customer was created successfully; false if email already exists</returns>
        public async Task<bool> CreateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                if (env == null)
                    throw new ArgumentNullException(nameof(env));

                _logger.LogInformation("Creating customer: {Email}", model.Email);

                if (await _context.Customers.AnyAsync(c => c.Email == model.Email))
                {
                    _logger.LogWarning("Customer with email {Email} already exists", model.Email);
                    return false;
                }

                var customer = new Customer
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.Address,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                if (model.ProfileImage != null)
                {
                    try
                    {
                        const string folder = "images/customers/";
                        var fileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                        var fullPath = Path.Combine(env.WebRootPath, folder, fileName);
                        Directory.CreateDirectory(Path.Combine(env.WebRootPath, folder));

                        using var stream = new FileStream(fullPath, FileMode.Create);
                        await model.ProfileImage.CopyToAsync(stream);
                        customer.ProfileImagePath = "/" + folder + fileName;

                        _logger.LogInformation("Uploaded profile image for customer: {FileName}", fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading profile image for customer");
                        throw;
                    }
                }

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer created successfully: {Email}", model.Email);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating customer");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating customer");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing customer.
        /// </summary>
        /// <param name="model">The customer view model with updated data</param>
        /// <param name="env">The web host environment</param>
        /// <returns>True if customer was updated successfully; false if not found or email already exists</returns>
        public async Task<bool> UpdateAsync(CustomerViewModel model, IWebHostEnvironment env)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                if (env == null)
                    throw new ArgumentNullException(nameof(env));

                _logger.LogInformation("Updating customer ID: {Id}", model.Id);

                var customer = await _context.Customers.FindAsync(model.Id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for update: {Id}", model.Id);
                    return false;
                }

                if (await _context.Customers.AnyAsync(c => c.Email == model.Email && c.Id != model.Id))
                {
                    _logger.LogWarning("Email already exists for another customer: {Email}", model.Email);
                    return false;
                }

                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Phone = model.Phone;
                customer.Address = model.Address;
                customer.IsActive = model.IsActive;

                if (model.ProfileImage != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(customer.ProfileImagePath))
                        {
                            var oldPath = Path.Combine(env.WebRootPath, customer.ProfileImagePath.TrimStart('/'));
                            if (File.Exists(oldPath)) 
                                File.Delete(oldPath);
                        }

                        const string folder = "images/customers/";
                        var fileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                        var fullPath = Path.Combine(env.WebRootPath, folder, fileName);
                        Directory.CreateDirectory(Path.Combine(env.WebRootPath, folder));

                        using var stream = new FileStream(fullPath, FileMode.Create);
                        await model.ProfileImage.CopyToAsync(stream);
                        customer.ProfileImagePath = "/" + folder + fileName;

                        _logger.LogInformation("Updated profile image for customer: {Id}", model.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating profile image for customer: {Id}", model.Id);
                        throw;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer updated successfully: {Id}", model.Id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating customer");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating customer");
                throw;
            }
        }

        /// <summary>
        /// Deletes a customer if they have no orders.
        /// </summary>
        /// <param name="id">The customer ID to delete</param>
        /// <param name="env">The web host environment</param>
        /// <returns>True if customer was deleted; false if not found or has orders</returns>
        public async Task<bool> DeleteAsync(int id, IWebHostEnvironment env)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid customer ID for deletion: {Id}", id);
                    return false;
                }

                if (env == null)
                    throw new ArgumentNullException(nameof(env));

                _logger.LogInformation("Deleting customer ID: {Id}", id);

                var customer = await _context.Customers
                    .Include(c => c.Orders)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for deletion: {Id}", id);
                    return false;
                }

                if (customer.Orders.Any())
                {
                    _logger.LogWarning("Cannot delete customer with existing orders: {Id}", id);
                    return false;
                }

                if (!string.IsNullOrEmpty(customer.ProfileImagePath))
                {
                    try
                    {
                        var oldPath = Path.Combine(env.WebRootPath, customer.ProfileImagePath.TrimStart('/'));
                        if (File.Exists(oldPath)) 
                            File.Delete(oldPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting profile image for customer: {Id}", id);
                    }
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer deleted successfully: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting customer");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting customer");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the order history for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>A list of orders for the customer</returns>
        public async Task<List<Order>> GetOrderHistoryAsync(int customerId)
        {
            try
            {
                if (customerId <= 0)
                {
                    _logger.LogWarning("Invalid customer ID for order history: {Id}", customerId);
                    return new List<Order>();
                }

                _logger.LogInformation("Retrieving order history for customer: {Id}", customerId);

                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {OrderCount} orders for customer: {Id}", orders.Count, customerId);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history for customer: {Id}", customerId);
                return new List<Order>();
            }
        }
    }
}