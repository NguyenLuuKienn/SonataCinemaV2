using SonataCinemaV2.Helper;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                
            var phim = db.LichChieux.Where(p => p.TrangThai == "Đang chiếu").Select(p => p.Phim.TenPhim).ToList();
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

                // Đổi ngày
                DateTime selectedDate;
                if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
                {
                    return new HttpStatusCodeResult(400, "Ngày không hợp lệ");
                }

                // Đổi giờ
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

        private const int SEAT_HOLD_MINUTES = 3;
        // lấy ghế
        [HttpGet]
        public ActionResult GetSeats(int room, int lichChieuId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetSeats called with room={room}, lichChieuId={lichChieuId}");
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

                var bookedSeats = db.Ves
                    .Where(v => v.ID_LichChieu == lichChieuId)
                    .Select(v => v.ChoNgoi)
                    .ToList();

                // Xóa các ghế giữ quá hạn
                var expiredTime = DateTime.Now.AddMinutes(-SEAT_HOLD_MINUTES);
                var expiredHolds = db.Ghe_TrangThai
                    .Where(gt => gt.ThoiGianGiu <= expiredTime)
                    .ToList();
                db.Ghe_TrangThai.RemoveRange(expiredHolds);
                db.SaveChanges();

                // Lấy danh sách ghế
                var seats = db.Ghes
                    .Where(g => g.ID_Phong == room)
                    .Select(g => new GheViewModel
                    {
                        IDGhe = g.ID_Ghe,
                        TenGhe = g.TenGhe,
                        TrangThai = db.Ves.Any(v =>
                            v.ID_LichChieu == lichChieuId &&
                            v.ChoNgoi == g.TenGhe) ? "Đã đặt" :
                            db.Ghe_TrangThai.Any(gt =>
                                gt.ID_Ghe == g.ID_Ghe &&
                                gt.ID_LichChieu == lichChieuId &&
                                gt.ThoiGianGiu > expiredTime) ? "Đang giữ" : "Trống"
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
                System.Diagnostics.Debug.WriteLine($"Error in GetSeats: {ex.Message}");
                return new HttpStatusCodeResult(500, "Lỗi khi tải danh sách ghế");
            }
        }

        [HttpPost]
        public ActionResult HoldSelectedSeats(int lichChieuId, List<int> selectedSeatIds)
        {
            try
            {
                foreach (var seatId in selectedSeatIds)
                {
                    var gheTrangThai = new Ghe_TrangThai
                    {
                        ID_LichChieu = lichChieuId,
                        ID_Ghe = seatId,
                        TrangThai = "Đang giữ",
                        ThoiGianGiu = DateTime.Now
                    };
                    db.Ghe_TrangThai.Add(gheTrangThai);
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetTicketPrice(int lichChieuId)
        {
            var lichChieu = db.LichChieux.Find(lichChieuId);
            if (lichChieu != null && lichChieu.GiaVe.HasValue)
            {
                return Json(lichChieu.GiaVe.Value, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
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
                .ToList()
                .Select(lc => new LichChieuViewModel
                {
                    IDLichChieu = lc.ID_LichChieu,
                    TenPhim = lc.TenPhim,
                    Ngay = lc.NgayChieu.ToString("dd-MM-yyyy"),
                    IDPhong = lc.ID_Phong,
                    GioChieu = lc.GioChieu.ToString(@"hh\:mm") 
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
                    System.Diagnostics.Debug.WriteLine($"Received IDLichChieu: {model.IDLichChieu}");

                    var lichChieu = db.LichChieux.Find(model.IDLichChieu);
                    if (lichChieu == null)
                    {
                        throw new Exception("Không tìm thấy lịch chiếu");
                    }
                    model.GiaVe = lichChieu.GiaVe ?? 0;
                    model.TongTien = model.GiaVe * model.ChonGhe.Count;
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
        public async Task<ActionResult> ConfirmPayment(ConfirmViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Start processing payment:");
                        System.Diagnostics.Debug.WriteLine($"IDLichChieu: {model.IDLichChieu}");
                        System.Diagnostics.Debug.WriteLine($"IDKhachHang: {model.IDKhachHang}");
                        System.Diagnostics.Debug.WriteLine($"TongTien: {model.TongTien}");
                        System.Diagnostics.Debug.WriteLine($"Số ghế đã chọn: {model.ChonGhe?.Count ?? 0}");

                        // Kiểm tra lịch chiếu
                        var lichChieu = db.LichChieux.Find(model.IDLichChieu);
                        if (lichChieu == null)
                        {
                            throw new Exception($"Không tìm thấy lịch chiếu với ID: {model.IDLichChieu}");
                        }

                        // Kiểm tra khách hàng
                        var khachHang = db.KhachHangs.Find(model.IDKhachHang);
                        if (khachHang == null)
                        {
                            throw new Exception($"Không tìm thấy khách hàng với ID: {model.IDKhachHang}");
                        }

                        // Thêm thanh toán 
                        var newThanhToan = new ThanhToan
                        {
                            ID_KhachHang = model.IDKhachHang,
                            TongTienGoc = model.TongTien,
                            SoTienGiam = 0,
                            ID_NhanVien = null,
                            NgayThanhToan = DateTime.Now
                        };
                        db.ThanhToans.Add(newThanhToan);
                        db.SaveChanges();

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
                                ID_ThanhToan = newThanhToan.ID_ThanhToan, 
                                Gia = lichChieu.GiaVe?? 0,
                                ChoNgoi = ghe.TenGhe,
                                NgayDat = DateTime.Now,
                                TrangThai = "Thành Công"
                            };
                            db.Ves.Add(newVe);
                            var gheTrangThai = await db.Ghe_TrangThai.FirstOrDefaultAsync(gt =>
                                gt.ID_LichChieu == model.IDLichChieu &&
                                gt.ID_Ghe == ghe.IDGhe);

                            if (gheTrangThai != null)
                            {
                                db.Ghe_TrangThai.Remove(gheTrangThai);
                            }
                        }
                        khachHang.DiemThuong += 1;
                        db.Entry(khachHang).State = EntityState.Modified;

                        await db.SaveChangesAsync();

                        // Gửi email xác nhận
                        var email = khachHang.Email;
                        var customerName = khachHang.TenKhachHang;
                        var movieName = lichChieu.Phim?.TenPhim;
                        var formattedDate = lichChieu.NgayChieu.ToString("dd/MM/yyyy");
                        var formattedTime = lichChieu.GioChieu.ToString(@"hh\:mm");
                        var showTime = $"{formattedDate} {formattedTime}";
                        var seats = string.Join(", ", model.ChonGhe.Select(g => g.TenGhe));
                        var totalAmount = model.TongTien;

                        await EmailHelper.SendBookingConfirmationEmail(
                            email,
                            customerName,
                            movieName,
                            showTime,
                            seats,
                            totalAmount
                        );


                        transaction.Commit();

                        TempData["Success"] = "Đặt vé thành công!";
                        return RedirectToAction("BookingSuccess");
                    }
                    catch (DbUpdateException dbEx)
                    {
                        transaction.Rollback();
                        // Log chi tiết lỗi 
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
                        System.Diagnostics.Debug.WriteLine($"Error Details:");
                        System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
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