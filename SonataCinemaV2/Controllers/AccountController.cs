using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
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

        // POST: Account
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(UserLogin model, string ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra tài khoản khách hàng
                var userKH = db.KhachHangs.SingleOrDefault(k => k.Email == model.Email);
                if (userKH != null && userKH.MatKhau == model.Password)
                {
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

                // Kiểm tra tài khoản nhân viên
                var userNV = db.NhanViens.FirstOrDefault(n => n.Email == model.Email);
                if (userNV != null && userNV.MatKhau == model.Password)
                {
                    FormsAuthentication.SetAuthCookie(userNV.Email, false);
                    Session["Admin"] = userNV.QuyenHan == "Admin";
                    Session["UserEmail"] = userNV.TenNhanVien;
                    Session["MaNhanVien"] = userNV.ID_NhanVien;

                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("Index", "Home")
                    });
                }

                // Nếu tài khoản hoặc mật khẩu sai, thêm lỗi vào ModelState
                ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_LoginPartial", model);
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