using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using CaptchaMvc.HtmlHelpers;
using Newtonsoft.Json.Linq;
using SonataCinemaV2.Helper;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinemaV2.Controllers
{
    public class AccountController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();
        // GET: Account

        [HttpGet]
        public ActionResult DangNhap(string ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            var model = new UserLogin();
            HttpCookie emailCookie = Request.Cookies["UserEmail"];
            if (emailCookie != null && !string.IsNullOrEmpty(emailCookie.Value))
            {
                model.Email = emailCookie.Value;
                model.RememberMe = true;
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult LoginPartial()
        {
            var model = new UserLogin();
            HttpCookie emailCookie = Request.Cookies["UserEmail"];
            if (emailCookie != null && !string.IsNullOrEmpty(emailCookie.Value))
            {
                model.Email = emailCookie.Value;
                model.RememberMe = true;
            }
            return PartialView("_LoginPartial", model);
        }

        // Captcha google
        public class ReCaptchaHelper
        {
            public static bool VerifyReCaptcha(string response)
            {
                try
                {
                    string secret = "6LeLSrIqAAAAAO7dxwA7JLEIfJR4jEr3h00R-dMK"; // secret key
                    using (var client = new WebClient())
                    {
                        var result = client.DownloadString(string.Format(
                            "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}",
                            secret,
                            response
                        ));
                        var obj = JObject.Parse(result);
                        return (bool)obj.SelectToken("success");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"reCAPTCHA Error: {ex.Message}");
                    return false;
                }
            }
        }

        // POST: Account
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(UserLogin model, string ReturnUrl, bool RememberMe = false)
        {
            // Khởi tạo session đếm số lần đăng nhập sai nếu chưa có
            if (Session["LoginAttempts"] == null)
            {
                Session["LoginAttempts"] = 0;
            }

            int loginAttempts = (int)Session["LoginAttempts"];
            bool requiresCaptcha = loginAttempts >= 5;

            if (ModelState.IsValid)
            {
                // CHỈ KIỂM TRA CAPTCHA SAU 5 LẦN ĐĂNG NHẬP SAI
                if (requiresCaptcha)
                {
                    var response = Request["g-recaptcha-response"];
                    if (string.IsNullOrEmpty(response))
                    {
                        ModelState.AddModelError("ReCaptcha", "Vui lòng xác nhận captcha");
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { 
                                success = false, 
                                requiresCaptcha = true,
                                html = RenderPartialViewToString("_LoginPartial", model)
                            });
                        }
                        return View(model);
                    }
                    
                    bool isValidCaptcha = ReCaptchaHelper.VerifyReCaptcha(response);
                    if (!isValidCaptcha)
                    {
                        ModelState.AddModelError("ReCaptcha", "Xác thực captcha không thành công");
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { 
                                success = false, 
                                requiresCaptcha = true,
                                html = RenderPartialViewToString("_LoginPartial", model)
                            });
                        }
                        return View(model);
                    }
                }

                // Kiểm tra tài khoản khách hàng
                var userKH = db.KhachHangs.SingleOrDefault(k => k.Email == model.Email);
                if (userKH != null)
                {
                    if (userKH.TrangThai == "Khoá")
                    {
                        // TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                        Session["LoginAttempts"] = loginAttempts + 1;
                        
                        ModelState.AddModelError("", "Tài khoản này đã bị khóa. Vui lòng liên hệ admin để được hỗ trợ.");
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { 
                                success = false, 
                                requiresCaptcha = (loginAttempts + 1) >= 5,
                                html = RenderPartialViewToString("_LoginPartial", model)
                            });
                        }
                        return View(model);
                    }

                    if (userKH.DaXacThuc == false)
                    {
                        // Lưu thông tin để xác thực
                        Session["PendingVerificationUserId"] = userKH.ID_KhachHang;
                        Session["PendingVerificationEmail"] = userKH.Email;
                        Session["PendingVerificationName"] = userKH.TenKhachHang;
                        
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { 
                                success = false, 
                                requireVerification = true,
                                message = "Tài khoản chưa được xác thực email.",
                                redirectUrl = Url.Action("XacThucEmail", "Account")
                            });
                        }
                        return RedirectToAction("XacThucEmail");
                    }

                    if (userKH.TrangThai == "Hoạt Động")
                    {
                        try
                        {
                            bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, userKH.MatKhau);
                            if (isValidPassword)
                            {
                                // ĐĂNG NHẬP THÀNH CÔNG - RESET SỐ LẦN ĐĂNG NHẬP SAI
                                Session["LoginAttempts"] = 0;

                                FormsAuthentication.SetAuthCookie(userKH.Email, RememberMe);
                                if (RememberMe)
                                {
                                    HttpCookie emailCookie = new HttpCookie("UserEmail")
                                    {
                                        Value = userKH.Email,
                                        Expires = DateTime.Now.AddDays(30)
                                    };
                                    Response.Cookies.Add(emailCookie);
                                }
                                else
                                {
                                    if (Request.Cookies["UserEmail"] != null)
                                    {
                                        HttpCookie emailCookie = new HttpCookie("UserEmail")
                                        {
                                            Expires = DateTime.Now.AddDays(-1)
                                        };
                                        Response.Cookies.Add(emailCookie);
                                    }
                                }

                                Session["Admin"] = false;
                                Session["Staff"] = false;
                                Session["Customer"] = true;
                                Session["UserName"] = userKH.TenKhachHang;
                                Session["MaKhachHang"] = userKH.ID_KhachHang;

                                return Json(new
                                {
                                    success = true,
                                    redirectUrl = Url.Action("Index", "Home")
                                });
                            }
                            else
                            {
                                // MẬT KHẨU SAI - TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                                Session["LoginAttempts"] = loginAttempts + 1;
                            }
                        }
                        catch (BCrypt.Net.SaltParseException)
                        {
                            if (userKH.MatKhau == model.Password)
                            {
                                // ĐĂNG NHẬP THÀNH CÔNG - RESET SỐ LẦN ĐĂNG NHẬP SAI
                                Session["LoginAttempts"] = 0;

                                userKH.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.Password);
                                db.SaveChanges();
                                FormsAuthentication.SetAuthCookie(userKH.Email, false);
                                Session["Admin"] = false;
                                Session["UserName"] = userKH.TenKhachHang;
                                Session["MaKhachHang"] = userKH.ID_KhachHang;
                                return Json(new
                                {
                                    success = true,
                                    redirectUrl = Url.Action("Index", "Home")
                                });
                            }
                            else
                            {
                                // MẬT KHẨU SAI - TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                                Session["LoginAttempts"] = loginAttempts + 1;
                            }
                        }
                    }    
                    
                    
                }

                // Kiểm tra tài khoản nhân viên
                var userNV = db.NhanViens.FirstOrDefault(n => n.Email == model.Email);
                if (userNV != null)
                {
                    if (userNV.TrangThai == "Khoá")
                    {
                        // TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                        Session["LoginAttempts"] = loginAttempts + 1;

                        ModelState.AddModelError("", "Tài khoản này đã bị khóa. Vui lòng liên hệ admin để được hỗ trợ.");
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { 
                                success = false, 
                                requiresCaptcha = (loginAttempts + 1) >= 5,
                                html = RenderPartialViewToString("_LoginPartial", model)
                            });
                        }
                        return View(model);
                    }

                    try
                    {
                        bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, userNV.MatKhau);
                        if (isValidPassword)
                        {
                            // ĐĂNG NHẬP THÀNH CÔNG - RESET SỐ LẦN ĐĂNG NHẬP SAI
                            Session["LoginAttempts"] = 0;

                            FormsAuthentication.SetAuthCookie(userNV.Email, RememberMe);
                            if (RememberMe)
                            {
                                HttpCookie emailCookie = new HttpCookie("UserEmail")
                                {
                                    Value = userNV.Email,
                                    Expires = DateTime.Now.AddDays(30)
                                };
                                Response.Cookies.Add(emailCookie);
                            }
                            else
                            {
                                if (Request.Cookies["UserEmail"] != null)
                                {
                                    HttpCookie emailCookie = new HttpCookie("UserEmail")
                                    {
                                        Expires = DateTime.Now.AddDays(-1)
                                    };
                                    Response.Cookies.Add(emailCookie);
                                }
                            }

                            if (userNV.QuyenHan == "Admin")
                            {
                                Session["Admin"] = true;
                                Session["Staff"] = false;
                            }
                            else if(userNV.QuyenHan == "Staff")
                            {
                                Session["Admin"] = false;
                                Session["Staff"] = true;
                            }

                            Session["UserName"] = userNV.TenNhanVien;
                            Session["NhanVien"] = new
                            {
                                ID_NhanVien = userNV.ID_NhanVien,
                                TenNhanVien = userNV.TenNhanVien,
                                Email = userNV.Email,
                                QuyenHan = userNV.QuyenHan
                            };

                            return Json(new
                            {
                                success = true,
                                redirectUrl = Url.Action("Index", "Home")
                            });
                        }
                        else
                        {
                            // MẬT KHẨU SAI - TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                            Session["LoginAttempts"] = loginAttempts + 1;
                        }
                    }
                    catch (BCrypt.Net.SaltParseException)
                    {
                        if (userNV.MatKhau == model.Password)
                        {
                            // ĐĂNG NHẬP THÀNH CÔNG - RESET SỐ LẦN ĐĂNG NHẬP SAI
                            Session["LoginAttempts"] = 0;

                            userNV.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.Password);
                            db.SaveChanges();
                            FormsAuthentication.SetAuthCookie(userNV.Email, false);
                            if (userNV.QuyenHan == "Admin")
                            {
                                Session["Admin"] = true;
                                Session["Staff"] = false;
                            }
                            else if (userNV.QuyenHan == "Staff")
                            {
                                Session["Admin"] = false;
                                Session["Staff"] = true;
                            }

                            Session["UserName"] = userNV.TenNhanVien;
                            Session["NhanVien"] = new
                            {
                                ID_NhanVien = userNV.ID_NhanVien,
                                TenNhanVien = userNV.TenNhanVien,
                                Email = userNV.Email,
                                QuyenHan = userNV.QuyenHan
                            };

                            return Json(new
                            {
                                success = true,
                                redirectUrl = Url.Action("Index", "Home")
                            });
                        }
                        else
                        {
                            // MẬT KHẨU SAI - TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                            Session["LoginAttempts"] = loginAttempts + 1;
                        }
                    }
                    
                }
                // NỀU KHÔNG TÌM THẤY TÀI KHOẢN HOẶC SAI THÔNG TIN - TĂNG SỐ LẦN ĐĂNG NHẬP SAI
                if (userKH == null && userNV == null)
                {
                    Session["LoginAttempts"] = loginAttempts + 1;
                }

                ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            }

            if (Request.IsAjaxRequest())
            {
                if (!ModelState.IsValid)
                {
                    // Truyền thông tin có cần captcha hay không
                    return Json(new { 
                        success = false, 
                        requiresCaptcha = ((int)Session["LoginAttempts"]) >= 5,
                        html = RenderPartialViewToString("_LoginPartial", model)
                    });
                }
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }

            return View(model);
        }

        private string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        [HttpGet]
        public ActionResult ForgotPasswordPartial()
        {
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ForgotPasswordPartial", new ForgotPasswordViewModel());
            }
            return RedirectToAction("DangNhap");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra email và gửi link reset password
                    var userKH = db.KhachHangs.FirstOrDefault(k => k.Email == model.Email);
                    var userNV = db.NhanViens.FirstOrDefault(n => n.Email == model.Email);

                    if (userKH != null || userNV != null)
                    {
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                string token = Guid.NewGuid().ToString();
                                var expiryTime = DateTime.Now.AddHours(24);

                                if (userKH != null)
                                {
                                    userKH.ResetPasswordToken = token;
                                    userKH.ResetPasswordExpiry = expiryTime;
                                    db.Entry(userKH).State = EntityState.Modified;
                                }
                                else
                                {
                                    userNV.ResetPasswordToken = token;
                                    userNV.ResetPasswordExpiry = expiryTime;
                                    db.Entry(userNV).State = EntityState.Modified;
                                }

                                await db.SaveChangesAsync();

                                var resetLink = Url.Action("ResetPassword", "Account",
                                    new { email = model.Email, token = token }, protocol: Request.Url.Scheme);

                                await EmailHelper.SendResetPasswordEmail(model.Email, resetLink);

                                transaction.Commit();
                                return Json(new { success = true });
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine($"Error in ForgotPassword: {ex.Message}");
                                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.");
                            }
                        }
                    }

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ForgotPassword: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại sau.");
                }
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ForgotPasswordPartial", model);
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("DangNhap");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            // Kiểm tra token
            var userKH = db.KhachHangs.FirstOrDefault(k => k.Email == email && k.ResetPasswordToken == token);
            var userNV = db.NhanViens.FirstOrDefault(n => n.Email == email && n.ResetPasswordToken == token);

            if (userKH == null && userNV == null)
            {
                return RedirectToAction("DangNhap");
            }

            // Kiểm tra token còn hạn
            if (userKH != null && userKH.ResetPasswordExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "Link đặt lại mật khẩu đã hết hạn.");
                return RedirectToAction("DangNhap");
            }
            if (userNV != null && userNV.ResetPasswordExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "Link đặt lại mật khẩu đã hết hạn.");
                return RedirectToAction("DangNhap");
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userKH = db.KhachHangs.FirstOrDefault(k => k.Email == model.Email && k.ResetPasswordToken == model.Token);
                var userNV = db.NhanViens.FirstOrDefault(n => n.Email == model.Email && n.ResetPasswordToken == model.Token);

                if (userKH != null)
                {
                    if (userKH.ResetPasswordExpiry < DateTime.Now)
                    {
                        ModelState.AddModelError("", "Link đặt lại mật khẩu đã hết hạn.");
                        return View(model);
                    }

                    userKH.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    userKH.ResetPasswordToken = null;
                    userKH.ResetPasswordExpiry = null;
                    db.Entry(userKH).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    return RedirectToAction("ResetPasswordConfirmation");
                }
                else if (userNV != null)
                {
                    if (userNV.ResetPasswordExpiry < DateTime.Now)
                    {
                        ModelState.AddModelError("", "Link đặt lại mật khẩu đã hết hạn.");
                        return View(model);
                    }

                    userNV.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    userNV.ResetPasswordToken = null;
                    userNV.ResetPasswordExpiry = null;
                    db.Entry(userNV).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    return RedirectToAction("ResetPasswordConfirmation");
                }
                else
                {
                    ModelState.AddModelError("", "Token không hợp lệ.");
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // Hiển thị trang xác thực email
        public ActionResult XacThucEmail()
        {
            var email = Session["PendingVerificationEmail"] as string;
            var userId = Session["PendingVerificationUserId"] as int?;

            if (string.IsNullOrEmpty(email) || userId == null)
            {
                TempData["ErrorMessage"] = "Phiên xác thực đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Email = email;
            return View();
        }

        // Gửi mã xác thực
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GuiMaXacThuc()
        {
            try
            {
                var userId = Session["PendingVerificationUserId"] as int?;
                var email = Session["PendingVerificationEmail"] as string;
                var name = Session["PendingVerificationName"] as string;

                if (userId == null || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Phiên xác thực đã hết hạn." });
                }

                var khachHang = db.KhachHangs.Find(userId.Value);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });
                }

                if (khachHang.DaXacThuc == true)
                {
                    return Json(new { success = false, message = "Tài khoản đã được xác thực." });
                }

                // Tạo mã xác thực mới
                string verificationCode = EmailHelper.GenerateVerificationCode();

                // Cập nhật database
                khachHang.MaXacThuc = verificationCode;
                khachHang.ThoiGianXacThuc = DateTime.Now.AddMinutes(15);

                db.Entry(khachHang).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Gửi email
                await EmailHelper.SendVerificationEmail(email, name, verificationCode);

                System.Diagnostics.Debug.WriteLine($"Verification code sent: {verificationCode} to {email}");

                return Json(new
                {
                    success = true,
                    message = "Mã xác thực đã được gửi đến email của bạn!"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send verification error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Không thể gửi mã xác thực. Vui lòng thử lại."
                });
            }
        }

        // Xác thực mã
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XacThucMa(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code) || code.Length != 6)
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã 6 số!" });
                }

                var userId = Session["PendingVerificationUserId"] as int?;

                if (userId == null)
                {
                    return Json(new { success = false, message = "Phiên xác thực đã hết hạn." });
                }

                var khachHang = db.KhachHangs.Find(userId.Value);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });
                }

                // Kiểm tra mã xác thực
                if (string.IsNullOrEmpty(khachHang.MaXacThuc) || khachHang.MaXacThuc != code)
                {
                    return Json(new { success = false, message = "Mã xác thực không đúng!" });
                }

                // Kiểm tra thời gian hết hạn
                if (khachHang.ThoiGianXacThuc == null || DateTime.Now > khachHang.ThoiGianXacThuc)
                {
                    return Json(new { success = false, message = "Mã xác thực đã hết hạn!" });
                }

                // Cập nhật trạng thái xác thực
                khachHang.DaXacThuc = true;
                khachHang.MaXacThuc = null;
                khachHang.ThoiGianXacThuc = null;

                db.Entry(khachHang).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Đăng nhập người dùng luôn
                FormsAuthentication.SetAuthCookie(khachHang.Email, false);
                Session["Admin"] = false;
                Session["Staff"] = false;
                Session["Customer"] = true;
                Session["UserName"] = khachHang.TenKhachHang;
                Session["MaKhachHang"] = khachHang.ID_KhachHang;

                // Xóa session xác thực
                Session.Remove("PendingVerificationUserId");
                Session.Remove("PendingVerificationEmail");
                Session.Remove("PendingVerificationName");

                System.Diagnostics.Debug.WriteLine($"Email verification successful for user: {khachHang.Email}");

                return Json(new
                {
                    success = true,
                    message = "Xác thực thành công! Chào mừng bạn đến với Sonata Cinema!",
                    redirectUrl = Url.Action("Index", "Home")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Verification error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình xác thực."
                });
            }
        }

        public ActionResult DangXuat()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }
    }
}