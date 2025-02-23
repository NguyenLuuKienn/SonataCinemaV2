using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinemaV2.Quyen
{
    public class AdminOnlyAuthorize : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var db = new CinemaV3Entities();
            var userEmail = httpContext.User.Identity.Name;

            var nhanVien = db.NhanViens.FirstOrDefault(n => n.Email == userEmail);
            if (nhanVien != null && nhanVien.QuyenHan == "Admin")
            {
                return true;
            }

            return false;
        }
    }
}