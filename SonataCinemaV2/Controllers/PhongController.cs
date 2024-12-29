using SonataCinema.Quyen;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AdminAuthorize("Admin")]
    public class PhongController : Controller
    {
        // GET: Phong
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult DanhSachPhongChieuPartial()
        {
            List<PhongChieu> danhsachPhong = db.PhongChieux.ToList();

            return PartialView("DanhSachPhongChieuPartial", danhsachPhong);
        }
    }
}