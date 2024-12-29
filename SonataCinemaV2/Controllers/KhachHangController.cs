using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Win32;
using SonataCinema;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{
    public class KhachHangController : Controller
    {
        //GET: KhachHang
        CinemaV3Entities db = new CinemaV3Entities();

        // phần ds khách hàng
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

        [HttpPost]
        public ActionResult DangKy(Register model)
        {
            if (ModelState.IsValid)
            {
                var KHtontai = db.KhachHangs.Any(kh => kh.Email == model.Email);
                if (KHtontai)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
                else
                {
                    var userKH = new KhachHang
                    {
                        TenKhachHang = model.TenKhachHang,
                        Email = model.Email,
                        SoDienThoai = model.SoDienThoai,
                        GioiTinh = model.GioiTinh,
                        NgaySinh = model.NgaySinh,
                        MatKhau = model.MatKhau
                    };
                    db.KhachHangs.Add(userKH);
                    db.SaveChanges();
                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("Index", "Home")
                    });
                }
            }
            if (Request.IsAjaxRequest())
            {
                return PartialView("_RegisterPartial", model);
            }
            return View(model);
        }
        public ActionResult DangKyThanhCong()
        {
            return View();
        }


    }
}