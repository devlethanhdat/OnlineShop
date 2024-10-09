using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using OnlineShop.Models.Db;
using OnlineShop.Models.ViewModels;
using PayPal.Api;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PayPal;
using OnlineShop.Models;
namespace OnlineShop.Controllers
{
    public class CartController : Controller
    {
        private OnlineShopContext _context;
        private IHttpContextAccessor _httpContextAccessor;
        IConfiguration _configuration;

        public CartController(OnlineShopContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var result = GetProductsinCart();
            return View(result);
        }
        public IActionResult ClearCart()
        {
            Response.Cookies.Delete("Cart");
            return View("/");
        }
        public List<CartViewModel> GetCartItems()
        {
            List<CartViewModel> cartList = new List<CartViewModel>();
            var prevCartItemsString = Request.Cookies["Cart"];
            if (!string.IsNullOrEmpty(prevCartItemsString))
            {
                cartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemsString);
            }
            return cartList;
        }
        public List<ProductCartViewModel> GetProductsinCart()
        {
            var cartItems = GetCartItems();
            if (!cartItems.Any())
            {
                return null;
            }
            var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
            var products = _context.Products
                            .Where(p => cartItemProductIds.Contains(p.Id))
                            .ToList();
            List<ProductCartViewModel> result = new List<ProductCartViewModel>();
            foreach (var item in products)
            {
                var newItem = new ProductCartViewModel
                {
                    Id = item.Id,
                    ImageName = item.ImageName,
                    Price = item.Price,
                    Title = item.Title,
                    Count = cartItems.Single(x => x.ProductId == item.Id).Count,
                    RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count

                };
                result.Add(newItem);
            }

            return result;
        }
        [HttpPost]
        public IActionResult UpdateCart([FromBody] CartViewModel request)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
            {
                return NotFound();
            }
            var cartItems = GetCartItems();
            var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);
            if (foundProductInCart == null)
            {
                var newCartItem = new CartViewModel() { };
                newCartItem.ProductId = request.ProductId;
                newCartItem.Count = request.Count;
                cartItems.Add(newCartItem);
            }
            else
            {
                if (request.Count > 0)
                {
                    foundProductInCart.Count = request.Count;
                }
                else
                {
                    cartItems.Remove(foundProductInCart);
                }
            }
            var json = JsonConvert.SerializeObject(cartItems);
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(7);
            Response.Cookies.Append("Cart", json, option);

            var result = cartItems.Sum(x => x.Count);
            return new JsonResult(result);
        }
    
        public IActionResult SmallCart()
        {
            var result = GetProductsinCart();
            return PartialView(result);
        }
        [Authorize]
        public IActionResult Checkout()
        {
            var order = new Models.Db.Order();
            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }
            ViewData["Products"] = GetProductsinCart();
            return View(order);
        }
        [Authorize]
        [HttpPost]
        public IActionResult ApplyCouponCode([FromForm] string couponCode)
        {
            var order = new Models.Db.Order();
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);
            if (coupon != null)
            {
                order.CouponCode = coupon.Code;
                order.CouponDiscount = coupon.Discount;
            }
            else
            {
                ViewData["Products"] = GetProductsinCart();
                TempData["message"] = "Mã Không Tồn Tại!";
                return View("Checkout", order);
            }
            var shipping = _context.Settings?.FirstOrDefault()?.Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }
            ViewData["Products"] = GetProductsinCart();
            return View("Checkout", order);
        }
        [Authorize]
        [HttpPost]
        public IActionResult Checkout(Models.Db.Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Products"] = GetProductsinCart();
                return View(order);
            }
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = _context.Coupons.FirstOrDefault(x => x.Code == order.CouponCode);
                if (coupon != null)
                {
                    order.CouponCode = coupon.Code;
                    order.CouponDiscount = coupon.Discount;
                }
                else
                {
                    TempData["message"] = "Mã Không Tồn Tại!";
                    ViewData["Products"] = GetProductsinCart();
                    return View(order);
                }
            }
            var products = GetProductsinCart();

            order.Shipping = _context.Settings.First().Shipping;
            order.CreateDate = DateTime.Now;
            order.SubTotal = products.Sum(x => x.RowSumPrice);
            order.Total = order.SubTotal + order.Shipping ?? 0;
            order.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (order.CouponDiscount != null)
            {
                order.Total -= order.CouponDiscount;
            }
            _context.Orders.Add(order);
            _context.SaveChanges();


            List<OrderDetail> orderDetails = new List<OrderDetail>();
            foreach (var item in products)
            {
                OrderDetail orderDetailItem = new OrderDetail()
                {
                    Count = item.Count,
                    ProductTitle = item.Title,
                    ProductPrice = (decimal)item.Price,
                    OrderId = order.Id,
                    ProductId = item.Id
                };
                orderDetails.Add(orderDetailItem);
            }
            _context.OrderDetails.AddRange(orderDetails);
            _context.SaveChanges();
            return Redirect("/Cart/RedirectToPayPal?orderId=" + order.Id);

        }
        public IActionResult RedirectToPayPal(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null)
            {
                return View("Thanh Toán Thất Bại");
            }

            var orderDetails = _context.OrderDetails.Where(x => x.OrderId == orderId).ToList();

            var clientId = _configuration.GetValue<string>("PayPal:Key");
            var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");
            var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

            try
            {
                string baseURI = $"{Request.Scheme}://{Request.Host}/cart/PaypalReturn?";
                var guid = Guid.NewGuid().ToString();

                var payment = new Payment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "paypal" },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            description = $"Order {order.Id}",
                            invoice_number = Guid.NewGuid().ToString(),
                            amount = new Amount
                            {
                                currency = "VND",
                                total = order.Total?.ToString("F2")
                            },
                            item_list = new ItemList
                            {
                                items = orderDetails.Select(p => new Item
                                {
                                    name = p.ProductTitle,
                                    currency = "VND",
                                    price = p.ProductPrice.ToString("F2"),
                                    quantity = p.Count.ToString(),
                                    sku = p.ProductId.ToString(),
                                }).ToList(),

                            },


                        }

                    },
                    redirect_urls = new RedirectUrls
                    {
                        cancel_url = $"{baseURI}&Cancel=true",
                        return_url = $"{baseURI}orderId={order.Id}"
                    }
                };
                payment.transactions[0].item_list.items.Add(new Item
                {
                    name = "Shipping cost",
                    currency = "VND",
                    price = order.Shipping?.ToString("F2"),
                    quantity = "1",
                    sku = "1",
                });
                var createdPayment = payment.Create(apiContext);
                var approvalUrl = createdPayment.links.FirstOrDefault(l => l.rel.ToLower() == "approval_url");

                _httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                return Redirect(approvalUrl.ToString());
            } 
            catch (PayPal.PaymentsException ex)
            {
                // Ghi log thông tin chi tiết của response
                Console.WriteLine("PayPal Error Response: " + ex.Response);  // Đây sẽ in ra chi tiết phản hồi
                Console.WriteLine("PayPal Error Details: " + ex.Details);    // Ghi log chi tiết lỗi, nếu có
                Console.WriteLine("PayPal Error Message: " + ex.Message);    // Thông báo lỗi chung
                

                // In ra toàn bộ chi tiết phản hồi
                return Content("Error occurred while creating payment: " + ex.Response + " - " + ex.Message);
            }

        }
        public IActionResult PaypalReturn(int orderId, string PayerId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null)
            {
                return View("Thanh Toán Thất Bại!");

            }
            var clientId = _configuration.GetValue<string>("PayPal:Key");
            var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");
            var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);
            try
            {
                var paymentId = _httpContextAccessor.HttpContext.Session.GetString("payment");
                var paymentExcution = new PaymentExecution { payer_id = PayerId };
                var payment = new Payment { id = paymentId };

                var excutedPayment = payment.Execute(apiContext, paymentExcution);

                if (excutedPayment.state.ToLower() != "approved")
                {
                    return View("Thanh Toán Thất Bại!");
                }
                Response.Cookies.Delete("Cart");

                order.TransId = excutedPayment.transactions[0].related_resources[0].sale.id;
                order.Status = excutedPayment.state.ToLower();
                _context.SaveChanges();

                return View("Thanh Toán Thành Công");
            }
            catch (Exception)
            {

                return View("Thanh Toán Thất Bại!");
            }

        }
    }
}
