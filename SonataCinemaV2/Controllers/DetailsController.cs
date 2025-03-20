using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Models;
using SonataCinemaV2.Services;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{
    public class DetailsController : Controller
    {
        // GET: Details
        CinemaV3Entities db = new CinemaV3Entities();

        private readonly MovieRecommenderService _recommenderService;

        public DetailsController()
        {
            _recommenderService = new MovieRecommenderService(db);
        }


        public ActionResult DetailsPage(int id)
        {
            var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);
            if (phim == null)
            {
                return RedirectToAction("Error");
            }

            if (Session["MaKhachHang"] != null)
            {
                var userId = (int)Session["MaKhachHang"];
                var recommender = new MovieRecommenderService(db);
                phim.RandomMovie = recommender.GetMovieRecommendations(userId, 3);
            }
            else
            {
                phim.RandomMovie = db.Phims.OrderBy(r => Guid.NewGuid()).Take(3).ToList();
            }

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

        public ActionResult RatingPhim(int idPhim, int rating)
        {
            try
            {
                if(!User.Identity.IsAuthenticated)
                {
                    return Json(new { succes = false, message = "Vui lòng đăng nhập" });
                }

                var userId = (int)Session["MaKhachHang"];
                var phim = db.Phims.Find(idPhim);
                if (phim == null)
                {
                    return Json(new { succes = false, massage = "Không tìm thấy phim" });
                }

                var existingRating = db.DanhGias.FirstOrDefault(d => d.ID_KhachHang == userId && d.ID_Phim == idPhim);
                if(existingRating != null)
                {
                    existingRating.DiemDanhGia = rating;
                    existingRating.ThoiGianDanhGia = DateTime.Now;
                }
                else
                {
                    var newRating = new DanhGia
                    {
                        ID_KhachHang = userId,
                        ID_Phim = idPhim,
                        DiemDanhGia = rating,
                        ThoiGianDanhGia = DateTime.Now
                    };
                    db.DanhGias.Add(newRating);
                }

                var allRatings = db.DanhGias.Where(d => d.ID_Phim == idPhim).Select(d => (double?)d.DiemDanhGia);

                if (allRatings.Any())
                {
                    var averageRating = allRatings.Average();
                    phim.DanhGia = (float?)averageRating;
                }
                else
                {
                    phim.DanhGia = rating;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    newRating = phim.DanhGia.HasValue ? Math.Round(phim.DanhGia.Value, 1) : rating,
                    message = "Cảm ơn bạn đã đánh giá phim"
                });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đánh giá phim: {ex.Message}");
                return Json(new { succes = false, message = "Lỗi khi đánh giá phim" });
            }
        }
    }
}