using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinemaV2.Quyen
{
    public class AuthorizeRoles : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Kiểm tra đã đăng nhập chưa
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var db = new CinemaV3Entities();
            var userEmail = httpContext.User.Identity.Name;


            var nhanVien = db.NhanViens.FirstOrDefault(n => n.Email == userEmail);
            if (nhanVien != null)
            {
                if (nhanVien.QuyenHan == "Admin" || nhanVien.QuyenHan == "Staff")
                {
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine("Authorization failed");
            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectResult("~/Account/DangNhap");
            }
            else
            {
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
        }
    }
}