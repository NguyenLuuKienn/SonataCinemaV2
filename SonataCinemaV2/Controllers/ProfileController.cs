using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SonataCinemaV2.Controllers
{
    public class ProfileController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();
        // GET: Profile
        public ActionResult ProfilePage()
        {
            // Kiểm tra cả hai loại tài khoản
            if (Session["MaKhachHang"] == null && Session["MaNhanVien"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            // Xử lý cho khách hàng
            if (Session["MaKhachHang"] != null)
            {
                int userId = (int)Session["MaKhachHang"];
                var khachHang = db.KhachHangs.Find(userId);

                var tickets = db.Ves
                    .Include("LichChieu.Phim")
                    .Include("LichChieu.PhongChieu")
                    .Where(v => v.ID_KhachHang == userId)
                    .ToList();

                ViewBag.Tickets = tickets;
                ViewBag.UserType = "KhachHang";
                return View(khachHang);
            }
            // Xử lý cho nhân viên
            else
            {
                int staffId = (int)Session["MaNhanVien"];
                var nhanVien = db.NhanViens.Find(staffId);
                ViewBag.UserType = "NhanVien";
                ViewBag.Tickets = new List<Ve>(); // Nhân viên không có vé
                return View("IndexStaff", nhanVien);
            }
        }

        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (Session["MaKhachHang"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
            }

            int userId = (int)Session["MaKhachHang"];
            var khachHang = db.KhachHangs.Find(userId);

            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản!" });
            }

            if (khachHang.MatKhau != currentPassword)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu mới không khớp!" });
            }

            try
            {
                khachHang.MatKhau = newPassword;
                db.SaveChanges();
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetTicketDetail(int id)
        {
            try
            {
                var ticket = db.Ves
                    .Include("LichChieu.Phim")
                    .Include("LichChieu.PhongChieu")
                    .FirstOrDefault(v => v.ID_Ve == id);


                if (ticket == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vé!" }, JsonRequestBehavior.AllowGet);
                }

                var result = new
                {
                    success = true,
                    data = new
                    {
                        ticketId = ticket.ID_Ve,
                        movieName = ticket.LichChieu.Phim.TenPhim,
                        roomName = ticket.LichChieu.PhongChieu.TenPhong,
                        showDate = ticket.LichChieu.NgayChieu.ToString("dd/MM/yyyy"),
                        showTime = ticket.LichChieu.GioChieu.ToString(@"hh\:mm"),
                        seatNumber = ticket.ChoNgoi,
                        price = ticket.Gia
                    }
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}