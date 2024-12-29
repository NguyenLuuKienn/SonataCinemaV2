using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{
    public class DetailsController : Controller
    {
        // GET: Details
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult DetailsPage(int id)
        {
            var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);
            if (phim == null)
            {
                return Redirect("Error");
            }
            var randomPhim = db.Phims.OrderBy(r => Guid.NewGuid()).Take(3).ToList();
            if (randomPhim == null || randomPhim.Count == 0)
            {
                randomPhim = new List<Phim>();
            }
            phim.RandomMovie = randomPhim;
            return View(phim);
        }

        public ActionResult filterCategory(string theloai)
        {
            if (!string.IsNullOrEmpty(theloai))
            {
                theloai = theloai.Trim();
            }
            else
            {
                ViewBag.Message = "Không có thể loại được chọn.";
                return View(new List<Phim>());
            }


            List<Phim> listPhim = db.Phims.Where(p => p.TheLoai.ToString().Contains(theloai)).OrderBy(p => p.ID_Phim).ToList();

            if (listPhim.Count == 0)
            {
                ViewBag.Message = "Không có phim nào thuộc thể loại này.";
            }
            else
            {
                ViewBag.Message = $"Phim thuộc thể loại: {theloai}";
            }
            return View(listPhim);

        }
    }
}