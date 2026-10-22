using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OrderListViewModel> GetPagedAsync(string? search, OrderStatus? status, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

            var query = _context.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o => o.OrderNumber.Contains(term) || o.CustomerName.Contains(term));
            }

            if (status.HasValue)
            {
                var statusText = status.Value.ToString();
                query = query.Where(o => o.OrderStatus == statusText);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerId = o.CustomerId,
                    CustomerName = o.CustomerName,
                    SubTotal = o.TotalAmount,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    Notes = o.Notes
                })
                .ToListAsync();

            return new OrderListViewModel
            {
                Orders = new PaginatedList<OrderViewModel>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                },
                SearchTerm = search,
                StatusFilter = status
            };
        }

        public async Task<OrderViewModel?> GetByIdAsync(int id)
        {
            var order = await _context.Orders.AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            var items = order.OrderItems
                .Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId ?? 0,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                })
                .ToList();

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                SubTotal = order.TotalAmount,
                DiscountAmount = 0,
                TaxAmount = 0,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                Notes = order.Notes,
                Items = items
            };
        }

        public async Task<OrderCreateViewModel> GetCreateModelAsync()
        {
            return new OrderCreateViewModel
            {
                Customers = await _context.Customers.AsNoTracking()
                    .OrderBy(c => c.FullName)
                    .Select(c => new CustomerOrderSelectDto
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        Email = c.Email
                    }).ToListAsync(),
                Products = await _context.Recipes.AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Select(p => new ProductOrderSelectDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        FinalPrice = p.Price,
                        StockQuantity = int.MaxValue
                    }).ToListAsync()
            };
        }

        public async Task<int> CreateAsync(OrderCreateViewModel model, string? userId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == model.CustomerId);
            if (customer == null) return 0;

            var lines = (model.Lines ?? new List<OrderLineDto>())
                .Where(l => l.ProductId > 0 && l.Quantity > 0)
                .GroupBy(l => l.ProductId)
                .Select(g => new OrderLineDto { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (lines.Count == 0) return 0;

            var recipeIds = lines.Select(l => l.ProductId).ToList();
            var recipes = await _context.Recipes.AsNoTracking()
                .Where(r => recipeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            var order = new Order
            {
                CustomerId = customer.Id,
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.Phone,
                DeliveryAddress = customer.Address,
                OrderNumber = await GenerateUniqueOrderNumberAsync(),
                OrderDate = DateTime.Now,
                PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "CashOnDelivery" : model.PaymentMethod,
                PaymentStatus = "Pending",
                OrderStatus = "Pending",
                Notes = model.Notes
            };

            foreach (var line in lines)
            {
                if (!recipes.TryGetValue(line.ProductId, out var recipe)) continue;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = null,
                    ProductName = recipe.Name,
                    Quantity = line.Quantity,
                    UnitPrice = recipe.Price
                });
            }

            if (order.OrderItems.Count == 0) return 0;

            order.TotalAmount = order.OrderItems.Sum(i => i.LineTotal);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            customer.LastActivityDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return order.Id;
        }

        public async Task UpdateStatusAsync(int id, OrderStatus orderStatus, PaymentStatus paymentStatus, string? userId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return;

            order.OrderStatus = orderStatus.ToString();
            order.PaymentStatus = paymentStatus.ToString();

            if (!string.IsNullOrWhiteSpace(notes))
            {
                order.Notes = notes.Trim().Length > 1000 ? notes.Trim()[..1000] : notes.Trim();
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateUniqueOrderNumberAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var number = OrderNumberGenerator.Generate();
                if (!await _context.Orders.AnyAsync(o => o.OrderNumber == number))
                {
                    return number;
                }
            }

            return $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}
