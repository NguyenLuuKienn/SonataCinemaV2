using SonataCinemaV2.ViewModel;
using SonataCinemaV2.Models;
using System;
using SonataCinemaV2.Quyen;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AuthorizeRoles]
    public class NhanVienController : Controller
    {
        // GET: NhanVien
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult Index()
        {
            return View();
        }
        // lấy danh sách nhân viên
        public ActionResult DanhSachNhanVienPartial()
        {
            List<NhanVien> danhsachNVien = db.NhanViens.ToList();

            return PartialView("DanhSachNhanVienPartial", danhsachNVien);
        }

        [AdminOnlyAuthorize]
        // thêm nhân viên
        [HttpPost]
        public ActionResult addEmploy(NvMoi nhanvienMoi)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    NhanVien nhanvien = new NhanVien
                    {
                        TenNhanVien = nhanvienMoi.TenNhanVien,
                        QuyenHan = "Staff",
                        MatKhau = nhanvienMoi.MatKhau,
                        Email = nhanvienMoi.Email,
                        TrangThai = "Hoạt động"
                    };

                    db.NhanViens.Add(nhanvien);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Thêm nhân viên thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public ActionResult StatusEmploy(int maNV)
        {
            try
            {
                var nhanvien = db.NhanViens.FirstOrDefault(nv => nv.ID_NhanVien == maNV);
                if (nhanvien == null)
                {
                    return Json(new { success = false, message = "Nhân viên không tồn tại!" });
                }

                // Đổi trạng thái
                nhanvien.TrangThai = nhanvien.TrangThai == "Khoá" ? "Hoạt động" : "Khoá";

                db.SaveChanges();
                string message = nhanvien.TrangThai == "Khoá" ?
                    "Khóa nhân viên thành công!" :
                    "Mở khóa nhân viên thành công!";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public ActionResult deleteEmploy(int maNV)
        {
            try
            {
                var nhanvien = db.NhanViens.FirstOrDefault(nv => nv.ID_NhanVien == maNV);
                if (nhanvien == null)
                {
                    return Json(new { success = false, message = "Nhân viên không tồn tại!" });
                }
                db.NhanViens.Remove(nhanvien);
                db.SaveChanges();
                return Json(new { success = true, message = "Xoá nhân viên thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // action lấy thông tin nhân viên
        [HttpGet]
        public JsonResult GetEmployee(int id)
        {
            try
            {
                var nhanvien = db.NhanViens.FirstOrDefault(nv => nv.ID_NhanVien == id);
                if (nhanvien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên!" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ID_NhanVien = nhanvien.ID_NhanVien,
                        TenNhanVien = nhanvien.TenNhanVien,
                        Email = nhanvien.Email,
                        QuyenHan = nhanvien.QuyenHan,
                        TrangThai = nhanvien.TrangThai
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // action cập nhật nhân viên
        [HttpPost]
        public JsonResult UpdateEmployee(NhanVien model)
        {
            try
            {
                var nhanvien = db.NhanViens.FirstOrDefault(nv => nv.ID_NhanVien == model.ID_NhanVien);
                if (nhanvien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên!" });
                }

                nhanvien.TenNhanVien = model.TenNhanVien;
                nhanvien.Email = model.Email;
                nhanvien.QuyenHan = model.QuyenHan;
                if (!string.IsNullOrEmpty(model.MatKhau))
                {
                    nhanvien.MatKhau = model.MatKhau;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}