using Ice_Cream_Parlour_Eproject.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class GalleryController : Controller
    {
        private readonly IGalleryService _galleryService;

        public GalleryController(IGalleryService galleryService)
        {
            _galleryService = galleryService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _galleryService.GetItemsAsync());
        }
    }
}
