using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;


namespace SonataCinema.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();
        // GET: Booking
        public ActionResult BookingTicket()
        {

            var phim = db.Phims.Select(p => p.TenPhim).ToList();
            var phong = db.PhongChieux.Select(pc => pc.TenPhong).ToList();
            var ngay = db.LichChieux.Select(lc => lc.NgayChieu).ToList().Select(d => d.ToString("dd-MM-yyyy")).ToList();
            var gio = db.LichChieux.Select(lc => lc.GioChieu).ToList().Select(t => t.ToString(@"hh\:mm")).ToList();

            var ghe = db.Ghes.Select(g => new GheViewModel { IDGhe = g.ID_Ghe, TenGhe = g.TenGhe }).ToList();

            var modell = new BookingViewModel
            {
                Phims = phim,
                PhongChieus = phong,
                Ngays = ngay,
                GioChieux = gio,
                DanhSachGhe = ghe,
            };
            ViewBag.QuickBooking = TempData["QuickBooking"];
            return View(modell);
        }
        // lấy phim
        [HttpGet]
        public ActionResult GetDates(string movie)
        {
            if (string.IsNullOrEmpty(movie))
            {
                return new HttpStatusCodeResult(400, "Tên phim không hợp lệ");
            }

            var dates = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == movie)
                .Select(lc => lc.NgayChieu)
                .Distinct()
                .ToList();

            if (!dates.Any())
            {
                return new HttpStatusCodeResult(404, "Không có lịch chiếu nào cho phim này");
            }

            var formatDate = dates.Select(d => d.ToString("dd-MM-yyyy")).ToList();
            return PartialView("_DatePartial", formatDate);
        }

        // lấy phòng
        [HttpGet]
        public ActionResult GetRooms(string movie, string date, string time)
        {
            try
            {
                // Debug
                System.Diagnostics.Debug.WriteLine($"Received - Movie: {movie}, Date: {date}, Time: {time}");

                // Parse date
                DateTime selectedDate;
                if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
                {
                    return new HttpStatusCodeResult(400, "Ngày không hợp lệ");
                }

                // Parse time
                var timeParts = time.Split(':');
                var timeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);

                // Debug
                System.Diagnostics.Debug.WriteLine($"Parsed - Date: {selectedDate}, Time: {timeSpan}");

                var availableRooms = db.LichChieux
                    .Where(lc => lc.Phim.TenPhim == movie &&
                                DbFunctions.TruncateTime(lc.NgayChieu) == selectedDate.Date &&
                                lc.GioChieu.Hours == timeSpan.Hours &&
                                lc.GioChieu.Minutes == timeSpan.Minutes)
                    .Select(lc => new
                    {
                        MaPhong = lc.ID_Phong,
                        TenPhong = lc.PhongChieu.TenPhong
                    })
                    .Distinct()
                    .ToList();

                // Debug
                System.Diagnostics.Debug.WriteLine($"Found rooms: {availableRooms.Count}");

                if (!availableRooms.Any())
                {
                    return Content("");
                }

                var options = availableRooms
                    .Select(r => $"<option value='{r.MaPhong}'>{r.TenPhong}</option>")
                    .ToList();

                return Content(string.Join("", options));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        // lấy giờ
        [HttpGet]
        public ActionResult GetTimes(string movie, string date)
        {
            DateTime selectDate;
            if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out selectDate))
            {
                return new HttpStatusCodeResult(400, "Ngày không hợp lệ");
            }

            var times = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == movie &&
                             DbFunctions.TruncateTime(lc.NgayChieu) == selectDate.Date)
                .Select(lc => lc.GioChieu)
                .Distinct()
                .ToList();

            var formatTime = times.Select(t => t.ToString(@"hh\:mm")).ToList();

            return PartialView("_TimePartial", formatTime);
        }
        // lấy ghế
        [HttpGet]
        public ActionResult GetSeats(int room, int lichChieuId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetSeats called with room={room}, lichChieuId={lichChieuId}");
                // Lấy thông tin lịch chiếu
                var lichChieu = db.LichChieux
                    .Where(lc => lc.ID_LichChieu == lichChieuId)
                    .Select(lc => new
                    {
                        lc.GioChieu,
                        ThoiLuong = lc.Phim.ThoiLuong ?? 0
                    })
                    .FirstOrDefault();

                if (lichChieu == null)
                {
                    return new HttpStatusCodeResult(404, "Không tìm thấy lịch chiếu");
                }

                // Lấy danh sách ghế đã đặt cho lịch chiếu này từ bảng Vé
                var bookedSeats = db.Ves
                    .Where(v => v.ID_LichChieu == lichChieuId)
                    .Select(v => v.ChoNgoi)
                    .ToList();

                // Lấy tất cả ghế của phòng
                var seats = db.Ghes
                    .Where(g => g.ID_Phong == room)
                    .Select(g => new GheViewModel
                    {
                        IDGhe = g.ID_Ghe,
                        TenGhe = g.TenGhe,
                        TrangThai = db.Ves.Any(v =>
                            v.ID_LichChieu == lichChieuId &&
                            v.ChoNgoi == g.TenGhe) ? "Đã đặt" : "Trống"
                    })
                    .OrderBy(g => g.TenGhe)
                    .ToList();

                if (!seats.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No seats found for room {room}");
                    return new HttpStatusCodeResult(404, "Không tìm thấy ghế cho phòng này");
                }

                return PartialView("_SeatPartial", seats);
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine($"Error in GetSeats: {ex.Message}");
                return new HttpStatusCodeResult(500, "Lỗi khi tải danh sách ghế");
            }
        }

        [HttpGet]
        public ActionResult GetAllLichChieu()
        {
            var lichChieuList = db.LichChieux
                .Select(lc => new
                {
                    lc.ID_LichChieu,
                    lc.Phim.TenPhim,
                    lc.NgayChieu,
                    lc.ID_Phong,
                    lc.GioChieu
                })
                .ToList() // Tải dữ liệu vào bộ nhớ
                .Select(lc => new LichChieuViewModel
                {
                    IDLichChieu = lc.ID_LichChieu,
                    TenPhim = lc.TenPhim,
                    Ngay = lc.NgayChieu.ToString("dd-MM-yyyy"), // Định dạng sau khi tải
                    IDPhong = lc.ID_Phong,
                    GioChieu = lc.GioChieu.ToString(@"hh\:mm") // Định dạng sau khi tải
                }).ToList();

            return PartialView("_LichChieuPartial", lichChieuList);
        }


        [HttpPost]
        public ActionResult ConfirmBooking(ConfirmViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Log để kiểm tra
                    System.Diagnostics.Debug.WriteLine($"Received IDLichChieu: {model.IDLichChieu}");

                    // Lưu vào TempData
                    TempData["BookingInfo"] = model;

                    return Json(new { redirectUrl = Url.Action("ConfirmBooking") });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                    return Json(new { error = ex.Message });
                }
            }

            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return Json(new { error = errors });
        }

        [HttpGet]
        public ActionResult ConfirmBooking()
        {
            var model = TempData["BookingInfo"] as ConfirmViewModel;
            if (model == null)
            {
                return RedirectToAction("BookingTicket");
            }

            // Log để kiểm tra
            System.Diagnostics.Debug.WriteLine($"Retrieved IDLichChieu: {model.IDLichChieu}");

            return View(model);
        }

        [HttpPost]
        public ActionResult ConfirmPayment(ConfirmViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Log thông tin đầu vào
                        System.Diagnostics.Debug.WriteLine($"Start processing payment:");
                        System.Diagnostics.Debug.WriteLine($"IDLichChieu: {model.IDLichChieu}");
                        System.Diagnostics.Debug.WriteLine($"IDKhachHang: {model.IDKhachHang}");
                        System.Diagnostics.Debug.WriteLine($"TongTien: {model.TongTien}");
                        System.Diagnostics.Debug.WriteLine($"Số ghế đã chọn: {model.ChonGhe?.Count ?? 0}");

                        // Kiểm tra tồn tại của lịch chiếu
                        var lichChieu = db.LichChieux.Find(model.IDLichChieu);
                        if (lichChieu == null)
                        {
                            throw new Exception($"Không tìm thấy lịch chiếu với ID: {model.IDLichChieu}");
                        }

                        // Kiểm tra tồn tại của khách hàng
                        var khachHang = db.KhachHangs.Find(model.IDKhachHang);
                        if (khachHang == null)
                        {
                            throw new Exception($"Không tìm thấy khách hàng với ID: {model.IDKhachHang}");
                        }

                        // Thêm thanh toán trước
                        var newThanhToan = new ThanhToan
                        {
                            ID_KhachHang = model.IDKhachHang,
                            TongTienGoc = model.TongTien,
                            SoTienGiam = 0,
                            ID_NhanVien = null,
                            NgayThanhToan = DateTime.Now
                        };
                        db.ThanhToans.Add(newThanhToan);
                        db.SaveChanges(); // Lưu thanh toán để lấy ID

                        // Thêm vé
                        foreach (var ghe in model.ChonGhe)
                        {
                            // Kiểm tra xem ghế đã được đặt chưa
                            var existingVe = db.Ves.FirstOrDefault(v =>
                                v.ID_LichChieu == model.IDLichChieu &&
                                v.ChoNgoi == ghe.TenGhe);

                            if (existingVe != null)
                            {
                                throw new Exception($"Ghế {ghe.TenGhe} đã được đặt");
                            }

                            var newVe = new Ve
                            {
                                ID_LichChieu = model.IDLichChieu,
                                ID_KhachHang = model.IDKhachHang,
                                ID_ThanhToan = newThanhToan.ID_ThanhToan, // Thêm ID thanh toán
                                Gia = 50000,
                                ChoNgoi = ghe.TenGhe,
                                DiemThuong = 0,
                                TrangThai = "Thành Công"
                            };
                            db.Ves.Add(newVe);
                        }

                        // Lưu tất cả thay đổi
                        db.SaveChanges();
                        transaction.Commit();

                        TempData["Success"] = "Đặt vé thành công!";
                        return RedirectToAction("BookingSuccess");
                    }
                    catch (DbUpdateException dbEx)
                    {
                        transaction.Rollback();
                        // Log chi tiết lỗi DbUpdateException
                        System.Diagnostics.Debug.WriteLine("DbUpdateException:");
                        System.Diagnostics.Debug.WriteLine($"Message: {dbEx.Message}");
                        if (dbEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                            if (dbEx.InnerException.InnerException != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Inner Inner Exception: {dbEx.InnerException.InnerException.Message}");
                            }
                        }
                        TempData["Error"] = "Lỗi cập nhật dữ liệu: " + dbEx.InnerException?.Message ?? dbEx.Message;
                        return RedirectToAction("BookingTicket");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"General Exception:");
                        System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }
                        TempData["Error"] = "Đã có lỗi xảy ra: " + ex.Message;
                        return RedirectToAction("BookingTicket");
                    }
                }
            }

            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            System.Diagnostics.Debug.WriteLine($"ModelState errors: {errors}");
            TempData["Error"] = "Dữ liệu không hợp lệ: " + errors;
            return RedirectToAction("BookingTicket");
        }


        public ActionResult BookingSuccess()
        {
            ViewBag.MovieName = TempData["MovieName"];
            ViewBag.ShowDate = TempData["ShowDate"];
            ViewBag.ShowTime = TempData["ShowTime"];
            ViewBag.SeatNumbers = TempData["SeatNumbers"];
            return View();
        }
    }
}