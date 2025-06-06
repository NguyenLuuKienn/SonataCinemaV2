﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CaptchaMvc.HtmlHelpers;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SonataCinema;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{
    public class KhachHangController : Controller
    {
        //GET: KhachHang
        CinemaV3Entities db = new CinemaV3Entities();

        public ActionResult DanhSachKhangHangPartial()
        {
            List<KhachHang> danhsachKH = db.KhachHangs.ToList();

            return PartialView("DanhSachKhangHangPartial", danhsachKH);
        }

        [HttpGet]
        public ActionResult DangKy()
        {
            var model = new Register();
            return View(model);
        }

        [HttpGet]
        public ActionResult RegisterPartial()
        {
            var model = new Register();
            return PartialView("_RegisterPartial", model);
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

        [HttpPost]
        public ActionResult DangKy(Register model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var response = Request["g-recaptcha-response"];
                    if (string.IsNullOrEmpty(response))
                    {
                        return Json(new { 
                            success = false, 
                            errors = new { ReCaptcha = "Vui lòng xác nhận captcha" } 
                        });
                    }

                    bool isValidCaptcha = ReCaptchaHelper.VerifyReCaptcha(response);
                    if (!isValidCaptcha)
                    {
                        return Json(new { 
                            success = false, 
                            errors = new { ReCaptcha = "Xác thực captcha không thành công" } 
                        });
                    }

                    var KHtontai = db.KhachHangs.Any(kh => kh.Email == model.Email);
                    if (KHtontai)
                    {
                        return Json(new { 
                            success = false, 
                            errors = new { Email = "Email đã được sử dụng" } 
                        });
                    }

                    string hashedPassWord = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);
                    var userKH = new KhachHang
                    {
                        TenKhachHang = model.TenKhachHang,
                        Email = model.Email,
                        SoDienThoai = model.SoDienThoai,
                        GioiTinh = model.GioiTinh,
                        TrangThai = "Hoạt Động",
                        NgaySinh = model.NgaySinh,
                        MatKhau = hashedPassWord,
                        QuyenHan = "Customer",
                        DiemThuong = 0,
                        DaXacThuc = false
                    };

                    db.KhachHangs.Add(userKH);
                    db.SaveChanges();
                    
                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("Index", "Home")
                    });
                }

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                );

                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                return Json(new { 
                    success = false, 
                    errors = new { General = "Đã xảy ra lỗi trong quá trình đăng ký" } 
                });
            }
        }

        [HttpPost]
        public ActionResult deleteCustomer (int idKH)
        {
            try
            {
                var khachhanng = db.KhachHangs.FirstOrDefault(k => k.ID_KhachHang == idKH);
                if(khachhanng == null)
                {
                    TempData["Message"] = "Khách hàng không tồn tại!";
                    return RedirectToAction("IndexAdmin", "Admin");
                }
                khachhanng.TrangThai = "Khoá";
                db.SaveChanges();
                TempData["Message"] = "Xóa khách hàng thành công!";
                return RedirectToAction("IndexAdmin", "Admin");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("IndexAdmin", "Admin");
            }
        }
        [HttpPost]
        public ActionResult deleteKH(int idKH)
        {
            try
            {
                var khachhanng = db.KhachHangs.FirstOrDefault(k => k.ID_KhachHang == idKH);
                if (khachhanng == null)
                {
                    return Json(new { success = false, message = "Khách hàng không tồn tại!" });
                }

                // Kiểm tra khách hàng có đang có vé đã đặt không
                var hasBookings = db.Ves.Any(v => v.ID_KhachHang == idKH);
                if (hasBookings)
                {
                    return Json(new { success = false, message = "Không thể xóa khách hàng đã có lịch sử đặt vé!" });
                }

                db.KhachHangs.Remove(khachhanng);
                db.SaveChanges();
                
                return Json(new { success = true, message = "Xóa khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting customer: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            try
            {
                var khachHang = db.KhachHangs.Find(id);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
                }

                System.Diagnostics.Debug.WriteLine($"Current status: {khachHang.TrangThai}");

                khachHang.TrangThai = khachHang.TrangThai == "Hoạt Động" ? "Khoá" : "Hoạt Động";
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"New status: {khachHang.TrangThai}");

                return Json(new
                {
                    success = true,
                    message = $"Đã thay đổi trạng thái thành {khachHang.TrangThai}",
                    newStatus = khachHang.TrangThai
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ToggleStatus: {ex.Message}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetCustomerDetails(int id)
        {
            try
            {
                var khachHang = db.KhachHangs.Find(id);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng!" }, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine($"Found customer: {khachHang.TenKhachHang}");

                var html = RenderPartialViewToString("_CustomerDetailsPartial", khachHang);
                return Json(new { success = true, html = html }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerDetails: {ex.Message}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

        public ActionResult DangKyThanhCong()
        {
            return View();
        }
    }
}