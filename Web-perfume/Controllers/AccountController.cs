using System;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Helpers;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class AccountController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // GET: /account/login
        public ActionResult Login(string returnUrl)
        {
            if (AuthHelper.IsLoggedIn(Session))
            {
                return RedirectToAction("Profile");
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /account/login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = db.Users.FirstOrDefault(u => u.Email == model.Email.Trim() && u.IsActive);
            if (user == null || user.PasswordHash != model.Password)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Save user to session
            AuthHelper.SetUser(Session, user);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.RoleID == 1) // Admin
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /account/register
        public ActionResult Register()
        {
            if (AuthHelper.IsLoggedIn(Session))
            {
                return RedirectToAction("Profile");
            }
            return View(new RegisterViewModel());
        }

        // POST: /account/register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            if (db.Users.Any(u => u.Email == model.Email.Trim()))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn email khác.");
                return View(model);
            }

            var newUser = new User
            {
                RoleID = 3, // Customer
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                PasswordHash = model.Password, // plain text in demo DB
                Phone = model.Phone,
                Address = model.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(newUser);
            db.SaveChanges();

            // Auto-login
            AuthHelper.SetUser(Session, newUser);

            return RedirectToAction("Profile");
        }

        // GET: /account/profile
        public new ActionResult Profile()
        {
            var sessionUser = AuthHelper.GetUser(Session);
            if (sessionUser == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
            }

            var user = db.Users.Find(sessionUser.UserID);
            if (user == null)
            {
                AuthHelper.Logout(Session);
                return RedirectToAction("Login");
            }

            var orders = db.Orders
                .Where(o => o.UserID == user.UserID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var model = new UserProfileViewModel
            {
                User = user,
                Orders = orders
            };

            return View(model);
        }

        // POST: /account/updateprofile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string fullName, string phone, string address)
        {
            var sessionUser = AuthHelper.GetUser(Session);
            if (sessionUser == null)
            {
                return RedirectToAction("Login");
            }

            var user = db.Users.Find(sessionUser.UserID);
            if (user != null)
            {
                user.FullName = fullName;
                user.Phone = phone;
                user.Address = address;
                db.SaveChanges();

                // Update session
                sessionUser.FullName = fullName;
                TempData["ProfileSuccess"] = "Cập nhật thông tin thành công!";
            }

            return RedirectToAction("Profile");
        }

        // GET: /account/logout
        public ActionResult Logout()
        {
            AuthHelper.Logout(Session);
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
