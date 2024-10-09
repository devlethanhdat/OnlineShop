using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using OnlineShop.Models.Db;
using OnlineShop.Models.ViewModels;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace OnlineShop.Controllers
{
    public class AccountController : Controller
    {
        private OnlineShopContext _context;

        public AccountController(OnlineShopContext context)
        {
            _context = context;
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {
            user.RegisterDate = DateTime.Now;
            user.IsAdmin = false;
            user.Email = user.Email?.Trim();
            user.Password = user.Password?.Trim();
            user.FullName = user.FullName?.Trim();
            user.RecoveryCode = 0;
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(user.Email);
            if (!match.Success)
            {
                ModelState.AddModelError("Email", "Email không hợp lệ");
                return View(user);
            }
            var prevUser = _context.Users.Any(x => x.Email == user.Email);
            if (prevUser)
            {
                ModelState.AddModelError("Email", "Email đả được sữ dụng");
                return View(user);
            }
            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("login");
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(LoginViewModel user)
        {
            if(!ModelState.IsValid)
            {
                return View(user);
            }
            var foundUser = _context.Users.FirstOrDefault(x => x.Email == user.Email.Trim() && x.Password == user.Password.Trim());
            if(foundUser == null)
            {
                ModelState.AddModelError("Email","Người Dùng Không Tồn Tại");
                return View(user);
            }
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, foundUser.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, foundUser.FullName));
            claims.Add(new Claim(ClaimTypes.Email, foundUser.Email));
            if(foundUser.IsAdmin == true)
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, "user"));
            }
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Redirect("/");


        }
        [Authorize]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        public IActionResult RecoveryPassword()
        {
            return View();
        }
        [HttpPost]
        public IActionResult RecoveryPassword(RecoveryPasswordViewModel recoveryPassword) 
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(recoveryPassword.Email);
            if (!match.Success)
            {
                ModelState.AddModelError("Email", "Email không hợp lệ");
                return View(recoveryPassword);
            }
            var foundUser = _context.Users.FirstOrDefault(x => x.Email == recoveryPassword.Email.Trim());
            if (foundUser == null)
            {
                ModelState.AddModelError("Email", "Người Dùng Không Tồn Tại");
                return View(recoveryPassword);
            }
            foundUser.RecoveryCode = new Random().Next(10000, 1000000);
            _context.Users.Update(foundUser);
            _context.SaveChanges();
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("datsexees@gmail.com");
            if (!string.IsNullOrEmpty(foundUser.Email) && foundUser.Email.Contains("@"))
            {
                mail.To.Add(foundUser.Email.Trim());

            }
            else
            {
                // Log lỗi hoặc hiển thị thông báo lỗi
                return BadRequest("Invalid email address.");
            }
            mail.Subject = "Rcovery Code";
            mail.Body = "Your Recovery Code " + foundUser.RecoveryCode;
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("datsexees@gmail.com", "bnku unqm gskp uvsv");
            SmtpServer.EnableSsl = true;
            try
            {
                SmtpServer.Send(mail);
            }
            catch (SmtpException ex)
            {
                // Log lỗi gửi email
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
            return Redirect("/Account/ResetPassword?email=" + foundUser.Email);
        }
        public IActionResult ResetPassword(string email)
        {
            var resetPasswordModel = new ResetPasswordViewModel();
            resetPasswordModel.Email = email;
            return View(resetPasswordModel);
        }
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel resetPassword)
        {
            if(!ModelState.IsValid)
            {
                return View(resetPassword);
            }
            var foundUser = _context.Users.FirstOrDefault(x => x.Email == resetPassword.Email && x.RecoveryCode == resetPassword.RecoveryCode);
            if(foundUser == null)
            {
                ModelState.AddModelError("Email", "Người Dùng Không Tồn Tại");
                return View(resetPassword);
            }
            foundUser.Password = resetPassword.NewPassword;
            _context.Users.Update(foundUser);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }
    }
}
