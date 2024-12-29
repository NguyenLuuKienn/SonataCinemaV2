using SonataCinema.Quyen;
using SonataCinemaV2.ViewModel;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinema.Controllers
{
    [AdminAuthorize("Admin")]
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


        // thêm nhân viên
        [HttpPost]
        public ActionResult addEmploy(NvMoi nhanvienMoi)
        {
            if (ModelState.IsValid)
            {
                NhanVien nhanvien = new NhanVien
                {
                    TenNhanVien = nhanvienMoi.TenNhanVien,
                    QuyenHan = nhanvienMoi.QuyenHan,
                    MatKhau = nhanvienMoi.MatKhau,
                    Email = nhanvienMoi.Email
                };

                db.NhanViens.Add(nhanvien);
                db.SaveChanges();
                return RedirectToAction("IndexAdmin", "Admin");
            }
            TempData["Error"] = "Dữ liệu không hợp lệ! Vui lòng kiểm tra lại.";
            return RedirectToAction("IndexAdmin", "Admin");
        }


        // xoá nhân viên
        [HttpPost]
        public ActionResult deleteEmploy(int maNV)
        {
            try
            {
                var nhanvien = db.NhanViens.FirstOrDefault(nv => nv.ID_NhanVien == maNV);
                if (nhanvien == null)
                {
                    TempData["Message"] = "Nhân viên không tồn tại!";
                    return RedirectToAction("IndexAdmin", "Admin");
                }
                db.NhanViens.Remove(nhanvien);
                db.SaveChanges();
                TempData["Message"] = "Xóa nhân viên thành công!";
                return RedirectToAction("IndexAdmin", "Admin");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("IndexAdmin", "Admin");
            }
        }
    }
}