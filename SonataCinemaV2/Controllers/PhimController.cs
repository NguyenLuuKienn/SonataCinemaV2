using SonataCinemaV2.ViewModel;
using SonataCinemaV2.Models;
using SonataCinemaV2.Quyen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AuthorizeRoles]
    public class PhimController : Controller
    {
        // GET: Phim
        CinemaV3Entities db = new CinemaV3Entities();

        public ActionResult DanhSachPhimPartial()
        {
            var danhsachPhim = db.Phims.ToList();
            var phimMoiList = danhsachPhim.Select(p => new PhimMoi
            {
                IDPhim = p.ID_Phim,
                TenPhim = p.TenPhim,
                TheLoai = p.TheLoai,
                DaoDien = p.DaoDien,
                DienVien = p.DienVien,
                NhaSanSuat = p.NhaSanSuat,
                ThoiLuong = p.ThoiLuong ?? 0,
                MoTa = p.MoTa,
                TrangThai = p.TrangThai,
                NoiBat = p.NoiBat ?? 0,
                TenPoster = p.Poster
            }).ToList();

            return PartialView("DanhSachPhimPartial", phimMoiList);
        }

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

        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult Create(PhimMoi phimMoi)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Phim phim = new Phim
                    {
                        TenPhim = phimMoi.TenPhim,
                        TheLoai = phimMoi.TheLoai,
                        DaoDien = phimMoi.DaoDien,
                        DienVien = phimMoi.DienVien,
                        NhaSanSuat = phimMoi.NhaSanSuat,
                        ThoiLuong = phimMoi.ThoiLuong,
                        MoTa = phimMoi.MoTa,
                        TrangThai = phimMoi.TrangThai,
                        NoiBat = phimMoi.NoiBat
                    };
                    if (phimMoi.Poster != null && phimMoi.Poster.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Poster.FileName);
                        string extension = Path.GetExtension(phimMoi.Poster.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        phim.Poster = fileName;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Poster.SaveAs(path);
                    }

                    if (phimMoi.Banner != null && phimMoi.Banner.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Banner.FileName);
                        string extension = Path.GetExtension(phimMoi.Banner.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        phim.Banner = fileName;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Banner.SaveAs(path);
                    }

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

                    return Json(new { success = true, message = "Thêm phim mới thành công!" });
                }
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);
                if (phim == null)
                {
                    return Json(new { success = true, message = "Không tìm thấy phim!" });
                }

                db.Phims.Remove(phim);
                db.SaveChanges();

                return Json(new { success = true, message = "Xóa phim thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminOnlyAuthorize]
        public ActionResult Edit(int id)
        {
            try
            {
                var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);
                if (phim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim!" }, JsonRequestBehavior.AllowGet);
                }

                var phimMoi = new PhimMoi
                {
                    IDPhim = phim.ID_Phim,
                    TenPhim = phim.TenPhim,
                    TheLoai = phim.TheLoai,
                    DaoDien = phim.DaoDien,
                    DienVien = phim.DienVien,
                    NhaSanSuat = phim.NhaSanSuat,
                    ThoiLuong = phim.ThoiLuong ?? 0,
                    MoTa = phim.MoTa,
                    TrangThai = phim.TrangThai,
                    NoiBat = phim.NoiBat ?? 0,
                    TenPoster = phim.Poster,
                    TenBanner = phim.Banner,
                    Trailer = phim.Trailer
                };
                return Json(phimMoi, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult Edit(PhimMoi phimMoi)
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

                    phim.TenPhim = phimMoi.TenPhim;
                    phim.TheLoai = phimMoi.TheLoai;
                    phim.DaoDien = phimMoi.DaoDien;
                    phim.DienVien = phimMoi.DienVien;
                    phim.NhaSanSuat = phimMoi.NhaSanSuat;
                    phim.ThoiLuong = phimMoi.ThoiLuong;
                    phim.MoTa = phimMoi.MoTa;
                    phim.TrangThai = phimMoi.TrangThai;
                    phim.NoiBat = phimMoi.NoiBat;

                    if (phimMoi.Poster != null && phimMoi.Poster.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Poster.FileName);
                        string extension = Path.GetExtension(phimMoi.Poster.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Poster.SaveAs(path);
                        phim.Poster = fileName;
                    }
                    else if (!string.IsNullOrEmpty(phimMoi.TenPoster))
                    {
                        phim.Poster = phimMoi.TenPoster;
                    }

                    if (phimMoi.Banner != null && phimMoi.Banner.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(phimMoi.Banner.FileName);
                        string extension = Path.GetExtension(phimMoi.Banner.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                        phimMoi.Banner.SaveAs(path);
                        phim.Banner = fileName;
                    }
                    else if (!string.IsNullOrEmpty(phimMoi.TenBanner))
                    {
                        phim.Banner = phimMoi.TenBanner;
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Cập nhật phim thành công!" });
                }
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetPhimById(int id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Getting movie with ID: {id}");

                var phim = db.Phims.FirstOrDefault(p => p.ID_Phim == id);

                if (phim != null)
                {

                    System.Diagnostics.Debug.WriteLine($"Found movie: {phim.TenPhim}");

                    var result = new
                    {
                        IDPhim = phim.ID_Phim,
                        TenPhim = phim.TenPhim ?? "",
                        TheLoai = phim.TheLoai ?? "",
                        DaoDien = phim.DaoDien ?? "",
                        DienVien = phim.DienVien ?? "",
                        NhaSanSuat = phim.NhaSanSuat ?? "",
                        ThoiLuong = phim.ThoiLuong ?? 0,
                        MoTa = phim.MoTa ?? "",
                        TrangThai = phim.TrangThai ?? "",
                        NoiBat = phim.NoiBat ?? 0,
                        TenPoster = phim.Poster,
                        TenBanner = phim.Banner,
                        Banner = phim.Banner ?? ""
                    };

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
        private bool IsValidImageFile(HttpPostedFileBase file)
        {
            if (file == null) return true;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return false;

            if (file.ContentLength > 5 * 1024 * 1024)
                return false;

            return true;
        }
    }
}