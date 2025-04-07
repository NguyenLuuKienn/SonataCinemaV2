using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

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
                ViewBag.Tickets = new List<Ve>();
                return View("IndexStaff", nhanVien);
            }
        }

        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
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

                // Kiểm tra mật khẩu hiện tại bằng BCrypt
                bool isValidPassword = BCrypt.Net.BCrypt.Verify(currentPassword, khachHang.MatKhau);
                if (!isValidPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu mới không khớp!" });
                }

                // Kiểm tra độ dài mật khẩu mới
                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự!" });
                }

                // Mã hóa mật khẩu mới bằng BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                khachHang.MatKhau = hashedPassword;
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"Password changed successfully for user {userId}");
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu: " + ex.Message });
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

        [HttpPost]
        public ActionResult UpdateProfile(string tenKhachHang, string email)
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

            try
            {
                khachHang.TenKhachHang = tenKhachHang;
                khachHang.Email = email;
                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult RequestCancelTicket(int ticketId)
        {
            if (Session["MaKhachHang"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
            }

            int maKhachHang = Convert.ToInt32(Session["MaKhachHang"]);

            if (ticketId <= 0)
            {
                return Json(new { success = false, message = "ID vé không hợp lệ!" });
            }

            try
            {
                var ve = db.Ves
                    .Include(v => v.LichChieu) // Đảm bảo load thông tin lịch chiếu
                    .FirstOrDefault(v => v.ID_Ve == ticketId);

                if (ve == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vé!" });
                }

                if (ve.ID_KhachHang != maKhachHang)
                {
                    return Json(new { success = false, message = "Bạn không có quyền huỷ vé này!" });
                }

                if (ve.TrangThai != "Thành Công")
                {
                    return Json(new { success = false, message = "Vé không thể huỷ!" });
                }

                // Lấy thời gian hiện tại
                var now = DateTime.Now;

                // Tính thời gian chiếu phim
                var showTime = ve.LichChieu.NgayChieu.Date + ve.LichChieu.GioChieu;

                // Kiểm tra xem phim đã chiếu chưa
                if (showTime < now)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể huỷ vé cho phim đã chiếu!"
                    });
                }

                // Tính thời gian còn lại đến lúc chiếu
                var timeUntilShow = showTime - now;

                // Kiểm tra xem còn ít nhất 30 phút trước khi chiếu
                if (timeUntilShow.TotalMinutes < 30)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Chỉ có thể huỷ vé trước giờ chiếu 30 phút!"
                    });
                }

                // Nếu thoả mãn tất cả điều kiện thì mới cho phép yêu cầu huỷ vé
                ve.TrangThai = "Chờ huỷ";
                db.SaveChanges();

                // Tính số tiền sẽ được hoàn (80%)
                decimal refundAmount = ve.Gia * 0.8m;

                return Json(new
                {
                    success = true,
                    message = $"Yêu cầu huỷ vé đã được gửi! Bạn sẽ được hoàn lại {refundAmount:N0} VNĐ sau khi yêu cầu được duyệt."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

    }
}