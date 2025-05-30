﻿using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;

namespace SonataCinemaV2.Controllers
{
    public class ProfileController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();
        // GET: Profile
        public ActionResult ProfilePage()
        {
            if (Session["MaKhachHang"] == null && Session["MaNhanVien"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

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

                bool isValidPassword = BCrypt.Net.BCrypt.Verify(currentPassword, khachHang.MatKhau);
                if (!isValidPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu mới không khớp!" });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự!" });
                }

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
        public async Task<ActionResult> RequestCancelTicket(int ticketId)
        {
            if (Session["MaKhachHang"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
            }

            int maKhachHang = Convert.ToInt32(Session["MaKhachHang"]);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var ve = db.Ves
                        .Include(v => v.LichChieu.Phim)
                        .Include(v => v.KhachHang)
                        .Include(v => v.ThanhToan)
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

                    var now = DateTime.Now;
                    var showTime = ve.LichChieu.NgayChieu.Date + ve.LichChieu.GioChieu;

                    if (showTime < now)
                    {
                        return Json(new { success = false, message = "Không thể huỷ vé cho phim đã chiếu!" });
                    }

                    var timeUntilShow = showTime - now;
                    if (timeUntilShow.TotalMinutes < 30)
                    {
                        return Json(new { success = false, message = "Chỉ có thể huỷ vé trước giờ chiếu 30 phút!" });
                    }

                    // hoàn trả 80%
                    decimal refundAmount = ve.ThanhToan.TongTienGoc * 0.8m;

                    var email = ve.KhachHang.Email;
                    var customerName = ve.KhachHang.TenKhachHang;
                    var movieName = ve.LichChieu.Phim.TenPhim;
                    var showTimeStr = $"{ve.LichChieu.NgayChieu:dd/MM/yyyy} {ve.LichChieu.GioChieu:hh\\:mm}";
                    var seats = ve.ChoNgoi;

                    ve.TrangThai = "Đã huỷ";
                    
                    await db.SaveChangesAsync();
                    transaction.Commit();

                    await Helper.EmailHelper.SendCancellationEmail(
                        email,
                        customerName,
                        movieName,
                        showTimeStr,
                        seats,
                        refundAmount
                    );

                    return Json(new
                    {
                        success = true,
                        message = $"Huỷ vé thành công! Email xác nhận đã được gửi. Bạn sẽ được hoàn lại {refundAmount:N0} VNĐ"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

    }
}