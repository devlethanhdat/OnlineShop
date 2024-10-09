using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;
using System.Text.RegularExpressions;

namespace OnlineShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly OnlineShopContext _context;

        public ProductsController(OnlineShopContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            List<Product> products = _context.Products.OrderByDescending(x => x.Id).ToList();
            return View(products);
        }
        public IActionResult SearchProducts(string SearchText) 
        {
            var products = _context.Products
                            .Where(x => 
                            EF.Functions.Like(x.Title, "%" + SearchText + "%") ||
                            EF.Functions.Like(x.Tags, "%" + SearchText + "%") ||
                            EF.Functions.Like(x.Brand, "%" + SearchText + "%")) 
                            .OrderBy(x => x.Title)
                            .ToList();
            return View("Index",products);
        }
        public IActionResult ProductDetails(int id)
        {
            Product? product = _context.Products.FirstOrDefault(x => x.Id == id);
            if(product == null)
            {
                return NotFound();
            }
            ViewData["gallery"] = _context.ProductGaleries.Where(x => x.ProductId == id ).ToList();
            ViewData["NewProducts"] = _context.Products.Where(x => x.Id != id).Take(10).OrderByDescending(x => x.Id).ToList();
            ViewData["comments"] = _context.Comments.Where(x => x.ProductId == id).OrderByDescending(x => x.CreateDate).ToList();

            return View(product);
        }
        [HttpPost]
        public IActionResult SubmitComment(string name, string email, string comment, int productId)
        {
            if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(comment) && productId != 0)
            {
                Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                Match match = regex.Match(email);
                if(!match.Success)
                {
                    TempData["ErrorMessage"] = "Email Is Not Valid";
                    return Redirect("/Products/ProductDetails/" + productId);
                }
                Comment newComment = new Comment();
                newComment.Name = name;
                newComment.Email = email;
                newComment.CommentText = comment;
                newComment.ProductId = productId;
                newComment.CreateDate = DateTime.Now;
                _context.Comments.Add(newComment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Bình Luận Thành Công!";
                return Redirect("/Products/ProductDetails/" + productId);

            }
            else
            {
                TempData["ErrorMessage"] = "Vui Lòng Nhập Đầy Đủ Thông Tin";
                return Redirect("/Products/ProductDetails/" + productId);
            }
        }
    }
}
