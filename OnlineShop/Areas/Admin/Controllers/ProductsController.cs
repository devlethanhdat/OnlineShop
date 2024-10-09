using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly OnlineShopContext _context;

        public ProductsController(OnlineShopContext context)
        {
            _context = context;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }
        public IActionResult DeleteGallery(int id)
        {
            var gallery = _context.ProductGaleries.FirstOrDefault(x => x.Id == id);
            if(gallery == null)
            {
                return NotFound();
            }
            string d = Directory.GetCurrentDirectory();
            string fn = d + "\\wwwroot\\images\\banners\\" + gallery.ImageName;
            if(System.IO.File.Exists(fn))
            {
                System.IO.File.Delete(fn);
            }
            _context.Remove(gallery);
            _context.SaveChanges();
            return Redirect("edit/" + gallery.ProductId) ;
        }
        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewBag.AvailableColors = new SelectList(new[] { "Red", "Blue", "Green", "Yellow", "Black", "White" });
            ViewBag.AvailableSize = new SelectList(new[] { "S", "M", "L", "XL", "XXL", "XXL" });

            return View();
        }

        // POST: Admin/Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Size,Qty,Tags,VideoUrl,Brand")] Product product, string[] Color, string[] Size, IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (ModelState.IsValid)
            {
                // Gộp các màu được chọn thành chuỗi (ví dụ: "Red,Blue,Green")
                if (Color != null && Color.Length > 0)
                {
                    product.Color = string.Join(",", Color);
                }
                if (Size != null && Size.Length > 0)
                {
                    product.Size = string.Join(",", Size);
                }

                // Xử lý ảnh chính
                if (MainImage != null)
                {
                    product.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(MainImage.FileName);
                    string fn = Directory.GetCurrentDirectory();
                    string imagePath = fn + "\\wwwroot\\images\\banners\\" + product.ImageName;
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        MainImage.CopyTo(stream);
                    }
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Xử lý gallery ảnh
                if (GalleryImages != null)
                {
                    foreach (var item in GalleryImages)
                    {
                        var newGallery = new ProductGalery
                        {
                            ProductId = product.Id,
                            ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(item.FileName)
                        };
                        string galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banners/", newGallery.ImageName);
                        using (var stream = new FileStream(galleryImagePath, FileMode.Create))
                        {
                            item.CopyTo(stream);
                        }
                        _context.ProductGaleries.Add(newGallery);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }


        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["gallery"] = _context.ProductGaleries.Where(x => x.ProductId == product.Id).ToList();
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Color,Size,Qty,Tags,VideoUrl,Brand")] Product product, IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if(MainImage != null)
                    {
                        string d = Directory.GetCurrentDirectory();
                        string fn = d + "\\wwwroot\\images\\banners\\" + product.ImageName;
                        if(System.IO.File.Exists(fn))
                        {
                            System.IO.File.Delete(fn);
                        }
                        using (var stream = new FileStream(fn, FileMode.Create))
                        {
                            MainImage.CopyTo(stream);
                        }

                    }
                    if (GalleryImages != null)
                    {
                        foreach (var item in GalleryImages)
                        {
                            var imageName = Guid.NewGuid() + Path.GetExtension(item.FileName);
                            string d = Directory.GetCurrentDirectory();
                            string fn = d + "\\wwwroot\\images\\banners\\" + imageName;
                            using (var stream = new FileStream(fn, FileMode.Create))
                            {
                                item.CopyTo(stream);
                            }
                            var galleryItem = new ProductGalery();
                            galleryItem.ImageName = imageName;
                            galleryItem.ProductId = product.Id;
                            _context.ProductGaleries.Add(galleryItem); 

                        }
                    }
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                string d = Directory.GetCurrentDirectory();
                string fn = d + "\\wwwroot\\images\\banners\\";
                string mainImagePath = fn + product.ImageName;
                if (System.IO.File.Exists(mainImagePath))
                {
                    System.IO.File.Delete(mainImagePath);
                }
                var galleries = _context.ProductGaleries.Where(x => x.ProductId == id).ToList();
                if(galleries != null)
                {
                    foreach (var item in galleries)
                    {
                        string galleryImagePath = fn + item.ImageName;
                        if (System.IO.File.Exists(galleryImagePath))
                        {
                            System.IO.File.Delete(galleryImagePath);
                        }
                    }
                    _context.ProductGaleries.RemoveRange(galleries);
                }

                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
