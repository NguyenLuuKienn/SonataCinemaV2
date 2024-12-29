using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.ViewModel;
using SonataCinema.Quyen;
using System.Web.Security;
using System.IO;
using System.Drawing;
using System.Data.Entity.Validation;
using SonataCinemaV2.Models;
namespace SonataCinema.Controllers
{
    [AdminAuthorize("Admin")]
    public class AdminController : Controller
    {
        // GET: Admin
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult IndexAdmin()
        {
            return View();
        }
        // phần toltal
        public ActionResult TotalPartial()
        {
            return PartialView("TotalPartial");
        }

        public ActionResult DangXuat()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}