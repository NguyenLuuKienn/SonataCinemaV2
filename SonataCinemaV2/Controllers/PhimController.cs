using SonataCinema.Quyen;
using SonataCinemaV2.ViewModel;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AdminAuthorize("Admin")]
    public class PhimController : Controller
    {
        // GET: Phim
        CinemaV3Entities db = new CinemaV3Entities();


        // phần ds phim
        public ActionResult DanhSachPhimPartial()
        {
            var danhsachPhim = db.Phims.ToList();
            var phimMoiList = danhsachPhim.Select(p => new PhimMoi
            {
                IDPhim = p.ID_Phim,
                TenPhim = p.TenPhim,
                TheLoai = p.TheLoai,
                ThoiLuong = p.ThoiLuong ?? 0,
                MoTa = p.MoTa,
                TrangThai = p.TrangThai,
                NoiBat = p.NoiBat ?? 0,
                TenPoster = p.Poster
            }).ToList();

            return PartialView("DanhSachPhimPartial", phimMoiList);
        }

        // lưu ảnh
        public string SaveImg(HttpPostedFileBase file, out string tenGoc)
        {
            if (file != null && file.ContentLength > 0)
            {
                tenGoc = Path.GetFileName(file.FileName);
                string uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                string path = Path.Combine(Server.MapPath("~/Content/img"), uniqueName);
                file.SaveAs(path);

                return uniqueName;
            }
            tenGoc = null;
            return null;
        }

        // thêm phim
        [HttpPost]
        public ActionResult Create(PhimMoi phimMoi)
        {
            if (ModelState.IsValid)
            {
                Phim phim = new Phim
                {
                    TenPhim = phimMoi.TenPhim,
                    TheLoai = phimMoi.TheLoai,
                    ThoiLuong = phimMoi.ThoiLuong,
                    MoTa = phimMoi.MoTa,
                    TrangThai = phimMoi.TrangThai,
                    NoiBat = phimMoi.NoiBat
                };
                // Xử lý Poster
                if (phimMoi.Poster != null && phimMoi.Poster.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(phimMoi.Poster.FileName);
                    string extension = Path.GetExtension(phimMoi.Poster.FileName);
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                    phim.Poster = fileName;
                    string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                    phimMoi.Poster.SaveAs(path);
                }

                // Xử lý Banner
                if (phimMoi.Banner != null && phimMoi.Banner.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(phimMoi.Banner.FileName);
                    string extension = Path.GetExtension(phimMoi.Banner.FileName);
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                    phim.Banner = fileName;
                    string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                    phimMoi.Banner.SaveAs(path);
                }
                // Xử lý URL trailer
                if (!string.IsNullOrEmpty(phimMoi.Trailer))
                {
                    try
                    {
                        if (phimMoi.Trailer.Contains("youtube.com/watch?v="))
                        {
                            phim.Trailer = phimMoi.Trailer.Replace("watch?v=", "embed/");
                            System.Diagnostics.Debug.WriteLine($"Converted watch URL: {phim.Trailer}");
                        }
                        else if (phimMoi.Trailer.Contains("youtu.be/"))
                        {
                            var videoId = phimMoi.Trailer.Split('/').Last();
                            phim.Trailer = $"https://www.youtube.com/embed/{videoId}";
                            System.Diagnostics.Debug.WriteLine($"Converted short URL: {phim.Trailer}");
                        }
                        else
                        {
                            phim.Trailer = phimMoi.Trailer;
                            System.Diagnostics.Debug.WriteLine($"Original URL: {phim.Trailer}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing trailer URL: {ex.Message}");
                    }
                }               

                db.Phims.Add(phim);
                db.SaveChanges();

                return RedirectToAction("IndexAdmin", "Admin");
            }
            TempData["Error"] = "Dữ liệu không hợp lệ! Vui lòng kiểm tra lại.";
            return RedirectToAction("IndexAdmin", "Admin");
        }

        // xoá phim
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);
                if (phim == null)
                {
                    TempData["Message"] = "Phim không tồn tại!";
                    return RedirectToAction("IndexAdmin", "Admin");
                }

                db.Phims.Remove(phim);
                db.SaveChanges();

                TempData["Message"] = "Xóa phim thành công!";
                return RedirectToAction("IndexAdmin", "Admin");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("IndexAdmin", "Admin");
            }
        }


        // sửa phim
        public ActionResult Edit(int id)
        {
            var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);

            PhimMoi phimMoi = new PhimMoi
            {
                IDPhim = phim.ID_Phim,
                TenPhim = phim.TenPhim,
                TheLoai = phim.TheLoai,
                ThoiLuong = phim.ThoiLuong ?? 0,
                MoTa = phim.MoTa,
                TrangThai = phim.TrangThai,
                NoiBat = phim.NoiBat ?? 0,
                TenPoster = phim.Poster,
                TenBanner = phim.Banner,
            };
            return View(phimMoi);
        }

        [HttpPost]
        public ActionResult Edit(PhimMoi phimMoi)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var phim = db.Phims.Find(phimMoi.IDPhim);
                    if (phim == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy phim!" });
                    }
                    // Cập nhật thông tin phim
                    phim.TenPhim = phimMoi.TenPhim;
                    phim.TheLoai = phimMoi.TheLoai;
                    phim.ThoiLuong = phimMoi.ThoiLuong;
                    phim.MoTa = phimMoi.MoTa;
                    phim.TrangThai = phimMoi.TrangThai;
                    phim.NoiBat = phimMoi.NoiBat;

                    // Xử lý URL trailer
                    if (!string.IsNullOrEmpty(phimMoi.Trailer))
                    {
                        // Kiểm tra và chuyển đổi URL YouTube
                        if (phimMoi.Trailer.Contains("youtube.com/watch?v="))
                        {
                            // Chuyển từ watch URL sang embed URL
                            phim.Trailer = phimMoi.Trailer.Replace("watch?v=", "embed/");
                        }
                        else if (phimMoi.Trailer.Contains("youtu.be/"))
                        {
                            // Chuyển từ short URL sang embed URL
                            var videoId = phimMoi.Trailer.Split('/').Last();
                            phim.Trailer = $"https://www.youtube.com/embed/{videoId}";
                        }
                        else
                        {
                            // Giữ nguyên URL nếu đã là dạng embed hoặc URL khác
                            phim.Trailer = phimMoi.Trailer;
                        }
                    }

                    // Xử lý upload ảnh mới nếu có
                    if (phimMoi.Poster != null && phimMoi.Poster.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Poster.FileName);
                        string extension = Path.GetExtension(phimMoi.Poster.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        phim.Poster = fileName;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Poster.SaveAs(path);
                    }
                    else
                    {
                        // Giữ nguyên tên file cũ nếu không upload file mới
                        phim.Poster = phimMoi.TenPoster;
                    }

                    // Xử lý Banner
                    if (phimMoi.Banner != null && phimMoi.Banner.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Banner.FileName);
                        string extension = Path.GetExtension(phimMoi.Banner.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        phim.Banner = fileName;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Banner.SaveAs(path);
                    }
                    else
                    {
                        // Giữ nguyên tên file cũ nếu không upload file mới
                        phim.Banner = phimMoi.TenBanner;
                    }


                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật phim thành công!";
                    return RedirectToAction("IndexAdmin", "Admin");
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
                System.Diagnostics.Debug.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!", errors = errors });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        // Thêm action này để lấy thông tin phim dạng JSON
        [HttpGet]
        public JsonResult GetPhimById(int id)
        {
            try
            {
                // Debug
                System.Diagnostics.Debug.WriteLine($"Getting movie with ID: {id}");

                var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);

                if (phim != null)
                {
                    // Debug
                    System.Diagnostics.Debug.WriteLine($"Found movie: {phim.TenPhim}");

                    // Đảm bảo trả về đúng định dạng dữ liệu
                    var result = new
                    {
                        IDPhim = phim.ID_Phim,
                        TenPhim = phim.TenPhim ?? "",
                        TheLoai = phim.TheLoai ?? "",
                        ThoiLuong = phim.ThoiLuong ?? 0,
                        MoTa = phim.MoTa ?? "",
                        TrangThai = phim.TrangThai ?? "",
                        NoiBat = phim.NoiBat ?? 0,
                        Poster = phim.Poster ?? "",
                        Banner = phim.Banner ?? ""
                    };

                    // Debug
                    System.Diagnostics.Debug.WriteLine($"Returning data: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");

                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine($"Movie not found with ID: {id}");
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}