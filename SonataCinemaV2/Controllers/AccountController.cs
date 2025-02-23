using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using CaptchaMvc.HtmlHelpers;
using Newtonsoft.Json.Linq;
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
            return View(model);
        }

        [HttpGet]
        public ActionResult LoginPartial()
        {
            var model = new UserLogin();
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
        public ActionResult DangNhap(UserLogin model, string ReturnUrl)
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
                                FormsAuthentication.SetAuthCookie(userKH.Email, false);
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
                            FormsAuthentication.SetAuthCookie(userNV.Email, false);
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

        public ActionResult DangXuat()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}