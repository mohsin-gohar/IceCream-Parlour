using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AllStaff)]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IWebHostEnvironment _environment;

    public CustomersController(ICustomerService customerService, IWebHostEnvironment environment)
    {
        _customerService = customerService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search, string sortBy = "name", string sortDir = "asc", int page = 1)
    {
        ViewData["Title"] = "Customers";
        var model = await _customerService.GetPagedAsync(search, sortBy, sortDir, page, 10);
        return View(model);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Add Customer";
        return View(new CustomerViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!await _customerService.CreateAsync(model, _environment))
        {
            ModelState.AddModelError("", "Email already exists or invalid data.");
            return View(model);
        }

        TempData["Success"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _customerService.GetByIdAsync(id);
        if (model == null) return NotFound();
        ViewData["Title"] = "Edit Customer";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!await _customerService.UpdateAsync(model, _environment))
        {
            ModelState.AddModelError("", "Update failed. Email may already exist.");
            return View(model);
        }

        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _customerService.GetByIdAsync(id);
        if (model == null) return NotFound();
        ViewData["Title"] = "Customer Details";
        ViewBag.Orders = await _customerService.GetOrderHistoryAsync(id);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _customerService.DeleteAsync(id, _environment);
        TempData[deleted ? "Success" : "Error"] = deleted ? "Customer deleted." : "Cannot delete customer with existing orders.";
        return RedirectToAction(nameof(Index));
    }

    
}
