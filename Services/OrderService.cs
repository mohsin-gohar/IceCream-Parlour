using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;

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
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.OrderNumber.Contains(search) || o.CustomerName.Contains(search));

            if (status.HasValue)
                query = query.Where(o => o.OrderStatus == status.Value.ToString());

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    Notes = o.Notes
                })
                .ToListAsync();

            return new OrderListViewModel
            {
                Orders = new PaginatedList<OrderViewModel> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                SearchTerm = search,
                StatusFilter = status
            };
        }

        public async Task<OrderViewModel?> GetByIdAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return null;

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                Notes = order.Notes
            };
        }

        public async Task<OrderCreateViewModel> GetCreateModelAsync()
        {
            return new OrderCreateViewModel
            {
                Customers = await _context.Customers.Select(c => new CustomerOrderSelectDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email
                }).ToListAsync(),
                Products = await _context.Recipes.Select(p => new ProductOrderSelectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    FinalPrice = 0,
                    StockQuantity = 9999
                }).ToListAsync()
            };
        }

        public async Task<int> CreateAsync(OrderCreateViewModel model, string? userId)
        {
            var order = new Areas.Models.Order
            {
                CustomerId = model.CustomerId,
                CustomerName = _context.Customers.Find(model.CustomerId)?.FullName ?? "",
                OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                OrderDate = DateTime.Now,
                TotalAmount = 0,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending",
                OrderStatus = "Pending",
                Notes = model.Notes
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order.Id;
        }

        public async Task UpdateStatusAsync(int id, OrderStatus orderStatus, PaymentStatus paymentStatus, string? userId, string? notes)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.OrderStatus = orderStatus.ToString();
                order.PaymentStatus = paymentStatus.ToString();
                await _context.SaveChangesAsync();
            }
        }
    }
}