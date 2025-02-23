using SonataCinemaV2.Quyen;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AuthorizeRoles]
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
        [HttpPost]
        public JsonResult ThemPhong(PhongChieu phong)
        {
            try
            {
                db.PhongChieux.Add(phong);
                db.SaveChanges();
                return Json(new { success = true, message = "Thêm phòng chiếu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaPhong(PhongChieu phong)
        {
            try
            {
                var existingPhong = db.PhongChieux.Find(phong.ID_Phong);
                if (existingPhong == null)
                    return Json(new { success = false, message = "Không tìm thấy phòng!" });

                existingPhong.TenPhong = phong.TenPhong;
                existingPhong.SoLuongGhe = phong.SoLuongGhe;
                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaPhong(int id)
        {
            try
            {
                var phong = db.PhongChieux.Find(id);
                if (phong == null)
                    return Json(new { success = false, message = "Không tìm thấy phòng!" });

                db.PhongChieux.Remove(phong);
                db.SaveChanges();
                return Json(new { success = true, message = "Xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}