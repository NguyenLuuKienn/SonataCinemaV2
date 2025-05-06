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
            ViewBag.RemainingMovies = db.Phims.OrderBy(n => n.TenPhim).Skip(6).ToList();
            return PartialView(product);
        }

        [HttpGet]
        public ActionResult LoadMoreMovies(int skip)
        {
            var nextMovies = db.Phims
                .OrderBy(n => n.TenPhim)
                .Skip(skip)
                .Take(8)
                .ToList();

            return PartialView("_MovieGridPartial", nextMovies);
        }
        public ActionResult DangChieu()
        {
            ViewBag.Title = "Phim Đang Chiếu";
            ViewBag.Link = "Phim Đang Chiếu";
            var DsDangChieu = db.Phims.Where(n => n.TrangThai == "Đang chiếu").ToList();

            return View(DsDangChieu);
        }
        public ActionResult SapChieu()
        {
            ViewBag.Title = "Phim Sắp Chiếu";
            ViewBag.Link = "Phim Sắp Chiếu";
            var DsSapChieu = db.Phims.Where(n => n.TrangThai == "Sắp chiếu").ToList();

            return View(DsSapChieu);
        }
    }
}
