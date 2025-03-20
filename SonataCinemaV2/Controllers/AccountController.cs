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
                    string secret = "6LeLSrIqAAAAAO7dxwA7JLEIfJR4jEr3h00R-dMK"; // Kiểm tra lại secret key
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
            if (ModelState.IsValid)
            {
                // kiểm tra captcha google
                var response = Request["g-recaptcha-response"];
                if (string.IsNullOrEmpty(response))
                {
                    ModelState.AddModelError("ReCaptcha", "Vui lòng xác nhận captcha");
                    if (Request.IsAjaxRequest())
                    {
                        return PartialView("_LoginPartial", model);
                    }
                    return View(model);
                }
                bool isValidCaptcha = ReCaptchaHelper.VerifyReCaptcha(response);
                if (!isValidCaptcha)
                {
                    ModelState.AddModelError("ReCaptcha", "Xác thực captcha không thành công");
                    if (Request.IsAjaxRequest())
                    {
                        return PartialView("_LoginPartial", model);
                    }
                    return View(model);
                }
                // Kiểm tra tài khoản khách hàng
                var userKH = db.KhachHangs.SingleOrDefault(k => k.Email == model.Email);
                if (userKH != null)
                {
                    if(userKH.TrangThai == "Khoá")
                    {
                        ModelState.AddModelError("", "Tài khoản này đã bị khóa. Vui lòng liên hệ admin để được hỗ trợ.");
                        if (Request.IsAjaxRequest())
                        {
                            return PartialView("_LoginPartial", model);
                        }
                        return View(model);
                    }    
                    if(userKH.TrangThai == "Hoạt Động")
                    {
                        try
                        {
                            bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, userKH.MatKhau);
                            if (isValidPassword)
                            {
                                FormsAuthentication.SetAuthCookie(userKH.Email, RememberMe);
                                if(RememberMe)
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
                        }
                        catch (BCrypt.Net.SaltParseException)
                        {
                            if (userKH.MatKhau == model.Password)
                            {
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
                        }
                    }    
                    
                    
                }

                // Kiểm tra tài khoản nhân viên
                var userNV = db.NhanViens.FirstOrDefault(n => n.Email == model.Email);
                if (userNV != null)
                {
                    if (userNV.TrangThai == "Khoá")
                    {
                        ModelState.AddModelError("", "Tài khoản này đã bị khóa. Vui lòng liên hệ admin để được hỗ trợ.");
                        if (Request.IsAjaxRequest())
                        {
                            return PartialView("_LoginPartial", model);
                        }
                        return View(model);
                    }
                    try
                    {
                        bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, userNV.MatKhau);
                        if (isValidPassword)
                        {
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
                    }
                    catch(BCrypt.Net.SaltParseException)
                    {
                        if(userNV.MatKhau == model.Password)
                        {
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
                                System.Diagnostics.Debug.WriteLine("Staff session set successfully");
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
                    }
                    
                }
                ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            }

            if (Request.IsAjaxRequest())
            {
                if (!ModelState.IsValid)
                {
                    return PartialView("_LoginPartial", model);
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

                    // Không tìm thấy email nhưng vẫn trả về thành công để tránh lộ thông tin
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

            // Kiểm tra token có hợp lệ không
            var userKH = db.KhachHangs.FirstOrDefault(k => k.Email == email && k.ResetPasswordToken == token);
            var userNV = db.NhanViens.FirstOrDefault(n => n.Email == email && n.ResetPasswordToken == token);

            if (userKH == null && userNV == null)
            {
                // Token không hợp lệ
                return RedirectToAction("DangNhap");
            }

            // Kiểm tra token còn hạn không
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

        public ActionResult DangXuat()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}