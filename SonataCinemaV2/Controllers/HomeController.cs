using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{

    public class HomeController : Controller
    {
        // GET: Home
        CinemaV3Entities db = new CinemaV3Entities();

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Slide()
        {
            var noibat = db.Phims.Where(n => n.NoiBat == 1).OrderBy(n => n.NoiBat);
            return PartialView(noibat);
        }

        public ActionResult Featured()
        {
            var noibat = db.Phims.Where(n => n.NoiBat == 2).OrderBy(n => n.NoiBat);
            return PartialView(noibat);
        }
        public ActionResult Product()
        {
            var product = db.Phims.OrderBy(n => n.TenPhim).ToList();
            return PartialView(product);
        }
        //phim dang chieu
        public ActionResult DangChieu()
        {
            var DsDangChieu = db.Phims.Where(n => n.TrangThai == "DangChieu").ToList();
            return View(DsDangChieu);
        }
        //phim sap chieu
        public ActionResult SapChieu()
        {
            var DsSapChieu = db.Phims.Where(n => n.TrangThai == "SapChieu").ToList();
            return View(DsSapChieu);
        }
    }
}
