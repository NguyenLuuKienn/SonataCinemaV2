using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Quyen
{
    public class AdminAuthorize : AuthorizeAttribute
    {
        private readonly string _role;
        public AdminAuthorize(string role)
        {
            _role = role;
        }
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var db = new CinemaV3Entities();
            var userEmail = httpContext.User.Identity.Name;
            var khachHang = db.KhachHangs.FirstOrDefault(k => k.Email == userEmail);
            if (khachHang != null && khachHang.QuyenHan == _role)
            {
                return true;
            }
            var nhanvien = db.NhanViens.FirstOrDefault(n => n.Email == userEmail);
            if (nhanvien != null && nhanvien.QuyenHan == _role)
            {
                return true;
            }
            return false;
        }
    }
}