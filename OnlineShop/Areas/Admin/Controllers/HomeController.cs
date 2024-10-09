using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
		private readonly OnlineShopContext _context;

		public HomeController(OnlineShopContext context)
		{
			
			_context = context;
		}

		public IActionResult Index()
        {
			var bestSellingProducts = _context.BestSellingFinals.ToList();
			ViewData["bestSellingProducts"] = bestSellingProducts;
			return View();
        }
    }
}
