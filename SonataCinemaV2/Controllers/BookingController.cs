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
            var today = DateTime.Today;
            var phim = db.LichChieux.Where(p => p.TrangThai == "Chưa chiếu" && p.NgayChieu >= today).Select(p => p.Phim.TenPhim).Distinct().ToList();
            var phong = db.PhongChieux.Select(pc => pc.TenPhong).ToList();
            var ngay = db.LichChieux.Select(lc => lc.NgayChieu).Distinct().ToList().Select(d => d.ToString("dd-MM-yyyy")).ToList();
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
        [HttpGet]
        public ActionResult GetDates(string movie)
        {
            if (string.IsNullOrEmpty(movie))
            {
                return new HttpStatusCodeResult(400, "Tên phim không hợp lệ");
            }

            var today = DateTime.Today;
            var dates = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == movie && lc.NgayChieu >= today)
                .Select(lc => lc.NgayChieu)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!dates.Any())
            {
                return new HttpStatusCodeResult(404, "Không có lịch chiếu sắp tới cho phim này");
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
        [HttpGet]
        public ActionResult GetSeats(int room, int lichChieuId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetSeats called with room={room}, lichChieuId={lichChieuId}");
                int currentUserId = Session.SessionID.GetHashCode();
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
                    .Where(v => v.ID_LichChieu == lichChieuId && v.TrangThai != "Đã huỷ")
                    .Select(v => v.ChoNgoi)
                    .ToList();

                var expiredTime = DateTime.Now.AddMinutes(-SEAT_HOLD_MINUTES);
                var expiredHolds = db.Ghe_TrangThai
                    .Where(gt => gt.ThoiGianGiu <= expiredTime)
                    .ToList();
                db.Ghe_TrangThai.RemoveRange(expiredHolds);
                db.SaveChanges();

                var seats = db.Ghes
                    .Where(g => g.ID_Phong == room)
                    .Select(g => new GheViewModel
                    {
                        IDGhe = g.ID_Ghe,
                        TenGhe = g.TenGhe,
                        TrangThai = db.Ves.Any(v =>
                           v.ID_LichChieu == lichChieuId &&
                            v.ChoNgoi == g.TenGhe &&
                            v.TrangThai != "Đã huỷ") ? "Đã đặt" :
                            db.Ghe_TrangThai.Any(gt =>
                                gt.ID_Ghe == g.ID_Ghe &&
                                gt.ID_LichChieu == lichChieuId &&
                                gt.ThoiGianGiu > expiredTime && gt.ID_KhachHang == currentUserId) ? "Đang giữ bởi bạn" : 
                                db.Ghe_TrangThai.Any(gt => gt.ID_Ghe == g.ID_Ghe && gt.ID_LichChieu == lichChieuId && gt.ThoiGianGiu > expiredTime) ? "Đang giữ" : "Trống",
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
                int currentUserId = Session.SessionID.GetHashCode();
                foreach (var seatId in selectedSeatIds)
                {
                    var gheTrangThai = new Ghe_TrangThai
                    {
                        ID_LichChieu = lichChieuId,
                        ID_Ghe = seatId,
                        TrangThai = "Đang giữ",
                        ThoiGianGiu = DateTime.Now,
                        ID_KhachHang = currentUserId
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
            try
            {
                System.Diagnostics.Debug.WriteLine("ConfirmBooking received data:");
                System.Diagnostics.Debug.WriteLine($"TongTienVe: {model.TongTienVe}");
                System.Diagnostics.Debug.WriteLine($"TongTienCombo: {model.TongTienCombo}");
                System.Diagnostics.Debug.WriteLine($"Combos count: {model.Combos?.Count ?? 0}");
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, error = errors });
                }

                // Lấy thông tin lịch chiếu
                var lichChieu = db.LichChieux
                    .Include(lc => lc.Phim)
                    .Include(lc => lc.PhongChieu)
                    .FirstOrDefault(lc => lc.ID_LichChieu == model.IDLichChieu);

                if (lichChieu == null)
                {
                    return Json(new { success = false, error = "Không tìm thấy lịch chiếu" });
                }

                var confirmModel = new ConfirmViewModel
                {
                    IDLichChieu = model.IDLichChieu,
                    IDKhachHang = model.IDKhachHang,
                    TenPhim = lichChieu.Phim.TenPhim,
                    TenPhong = lichChieu.PhongChieu.TenPhong,
                    Ngay = lichChieu.NgayChieu.ToString("dd-MM-yyyy"),
                    GioChieu = lichChieu.GioChieu.ToString(@"hh\:mm"),
                    GiaVe = lichChieu.GiaVe ?? 0,
                    ChonGhe = model.ChonGhe,
                    Combos = model.Combos,
                    TongTien = model.ChonGhe.Count * (lichChieu.GiaVe ?? 0),
                    TongTienCombo = model.TongTienCombo
                };

                confirmModel.TongTien = confirmModel.TongTien + confirmModel.TongTienCombo;

                System.Diagnostics.Debug.WriteLine($"Saving to TempData: {confirmModel.TenPhim}, {confirmModel.TenPhong}, {confirmModel.Ngay}");

                TempData["BookingInfo"] = confirmModel;

                return Json(new { success = true, redirectUrl = Url.Action("ConfirmBooking") });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ConfirmBooking: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ConfirmBooking()
        {
            var model = TempData["BookingInfo"] as ConfirmViewModel;
            if (model == null)
            {
                return RedirectToAction("BookingTicket");
            }

            System.Diagnostics.Debug.WriteLine($"Retrieved from TempData: {model.TenPhim}, {model.TenPhong}, {model.Ngay}");

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
                        System.Diagnostics.Debug.WriteLine($"Số combo đã chọn: {model.Combos?.Count ?? 0}");

                        // 1. Kiểm tra dữ liệu
                        var lichChieu = db.LichChieux.Find(model.IDLichChieu);
                        if (lichChieu == null)
                        {
                            throw new Exception($"Không tìm thấy lịch chiếu với ID: {model.IDLichChieu}");
                        }

                        var khachHang = db.KhachHangs.Find(model.IDKhachHang);
                        if (khachHang == null)
                        {
                            throw new Exception($"Không tìm thấy khách hàng với ID: {model.IDKhachHang}");
                        }

                        // 2. Tính tổng tiền
                        decimal tongTienVe = model.ChonGhe.Count * (lichChieu.GiaVe ?? 0);
                        decimal tongTienCombo = 0;
                        string comboInfo = "Không có combo";
                        if (model.Combos != null && model.Combos.Any())
                        {
                            tongTienCombo = model.Combos.Sum(c => c.SoLuong * c.Gia);
                            comboInfo = string.Join(", ", model.Combos.Select(c => $"{c.TenCombo} x{c.SoLuong}"));
                        }
                        decimal tongTienThanhToan = tongTienVe + tongTienCombo;

                        // 3. Tạo thanh toán
                        var newThanhToan = new ThanhToan
                        {
                            ID_KhachHang = model.IDKhachHang,
                            TongTienGoc = tongTienThanhToan,
                            SoTienGiam = 0, 
                            TongTienCombo = tongTienCombo,
                            ID_NhanVien = null,
                            NgayThanhToan = DateTime.Now
                        };
                        db.ThanhToans.Add(newThanhToan);
                        await db.SaveChangesAsync();

                        var ticketIds = new List<int>();
                        foreach (var ghe in model.ChonGhe)
                        {
                            var existingVe = db.Ves.FirstOrDefault(v =>
                                v.ID_LichChieu == model.IDLichChieu &&
                                v.ChoNgoi == ghe.TenGhe && v.TrangThai != "Đã huỷ");

                            if (existingVe != null)
                            {
                                throw new Exception($"Ghế {ghe.TenGhe} đã được đặt");
                            }

                            var newVe = new Ve
                            {
                                ID_LichChieu = model.IDLichChieu,
                                ID_KhachHang = model.IDKhachHang,
                                ID_ThanhToan = newThanhToan.ID_ThanhToan,
                                Gia = lichChieu.GiaVe ?? 0,
                                ChoNgoi = ghe.TenGhe,
                                NgayDat = DateTime.Now,
                                TrangThai = "Thành Công"
                            };
                            db.Ves.Add(newVe);
                            await db.SaveChangesAsync(); // Save to get the ticket ID
                            
                            ticketIds.Add(newVe.ID_Ve);

                            // Generate QR code for each ticket
                            var qrData = QRCodeHelper.GenerateTicketQRData(
                                newVe.ID_Ve,
                                khachHang.TenKhachHang,
                                lichChieu.Phim.TenPhim,
                                $"{lichChieu.NgayChieu:dd/MM/yyyy} {lichChieu.GioChieu:hh\\:mm}",
                                ghe.TenGhe
                            );

                            var qrFileName = $"ticket_{newVe.ID_Ve}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var qrCodePath = QRCodeHelper.GenerateQRCode(qrData, qrFileName);

                            if (!string.IsNullOrEmpty(qrCodePath))
                            {
                                // Update ticket with QR code path
                                newVe.QRCodePath = qrCodePath; // Add this field to your Ve model
                                db.Entry(newVe).State = EntityState.Modified;
                            }

                            // Remove held seat
                            var gheTrangThai = await db.Ghe_TrangThai.FirstOrDefaultAsync(gt =>
                                gt.ID_LichChieu == model.IDLichChieu &&
                                gt.ID_Ghe == ghe.IDGhe);

                            if (gheTrangThai != null)
                            {
                                db.Ghe_TrangThai.Remove(gheTrangThai);
                            }
                        }

                        // 5. Tạo combo orders
                        if (model.Combos != null && model.Combos.Any())
                        {
                            foreach (var combo in model.Combos)
                            {
                                var comboOrder = new ComboOrder
                                {
                                    ID_ThanhToan = newThanhToan.ID_ThanhToan,
                                    ID_Combo = combo.IDCombo,
                                    SoLuong = combo.SoLuong,
                                    GiaTien = combo.Gia * combo.SoLuong
                                };
                                db.ComboOrders.Add(comboOrder);
                            }
                        }

                        // 6. Cập nhật điểm thưởng
                        khachHang.DiemThuong += 1;
                        db.Entry(khachHang).State = EntityState.Modified;

                        await db.SaveChangesAsync();

                        // 7. Gửi email xác nhận
                        var email = khachHang.Email;
                        var customerName = khachHang.TenKhachHang;
                        var movieName = lichChieu.Phim?.TenPhim;
                        var formattedDate = lichChieu.NgayChieu.ToString("dd/MM/yyyy");
                        var formattedTime = lichChieu.GioChieu.ToString(@"hh\:mm");
                        var showTime = $"{formattedDate} {formattedTime}";
                        var seats = string.Join(", ", model.ChonGhe.Select(g => g.TenGhe));
                        var combos = model.Combos != null && model.Combos.Any()
                            ? string.Join(", ", model.Combos.Select(c => $"{c.TenCombo} x{c.SoLuong}"))
                            : "Không có combo";

                        await EmailHelper.SendBookingConfirmationEmail(
                            email,
                            customerName,
                            movieName,
                            showTime,
                            seats,
                            combos,
                            tongTienThanhToan
                        );

                        // 8. Save session data including QR codes
                        Session["BookingSuccess_MovieName"] = lichChieu.Phim.TenPhim;
                        Session["BookingSuccess_ShowDate"] = lichChieu.NgayChieu.ToString("dd/MM/yyyy");
                        Session["BookingSuccess_ShowTime"] = lichChieu.GioChieu.ToString(@"hh\:mm");
                        Session["BookingSuccess_SeatNumbers"] = string.Join(", ", model.ChonGhe.Select(g => g.TenGhe));
                        Session["BookingSuccess_RoomName"] = lichChieu.PhongChieu.TenPhong;
                        Session["BookingSuccess_Combos"] = comboInfo;
                        Session["BookingSuccess_TotalAmount"] = tongTienThanhToan.ToString("#,##0 VNĐ");
                        Session["BookingSuccess_TicketIds"] = ticketIds; // Add ticket IDs for QR display

                        transaction.Commit();

                        TempData["Success"] = "Đặt vé thành công!";
                        return RedirectToAction("BookingSuccess");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                        TempData["Error"] = "Đã có lỗi xảy ra: " + ex.Message;
                        return RedirectToAction("BookingTicket");
                    }
                }
            }

            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            TempData["Error"] = "Dữ liệu không hợp lệ: " + errors;
            return RedirectToAction("BookingTicket");
        }

        [HttpGet]
        public ActionResult GetCombos()
        {
            try
            {
                var combos = db.Combos
                    .Where(c => c.TrangThai == true)
                    .ToList();

                return PartialView("_ComboPartial", combos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCombos: {ex.Message}");
                return new HttpStatusCodeResult(500, "Không thể tải danh sách combo");
            }
        }
        [HttpPost]
        public ActionResult ReleaseSeat(int lichChieuId, List<int> gheIds)
        {
            try
            {
                var seatsToRelease = db.Ghe_TrangThai
                    .Where(gt => gt.ID_LichChieu == lichChieuId &&
                           gheIds.Contains(gt.ID_Ghe))
                    .ToList();

                db.Ghe_TrangThai.RemoveRange(seatsToRelease);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult BookingSuccess()
        {
            ViewBag.MovieName = Session["BookingSuccess_MovieName"];
            ViewBag.ShowDate = Session["BookingSuccess_ShowDate"];
            ViewBag.ShowTime = Session["BookingSuccess_ShowTime"];
            ViewBag.SeatNumbers = Session["BookingSuccess_SeatNumbers"];
            ViewBag.TotalAmount = Session["BookingSuccess_TotalAmount"];
            ViewBag.RoomName = Session["BookingSuccess_RoomName"];
            ViewBag.Combos = Session["BookingSuccess_Combos"];
            
            // Get QR codes for tickets - Fixed approach
            var ticketIds = Session["BookingSuccess_TicketIds"] as List<int>;
            if (ticketIds != null && ticketIds.Any())
            {
                try
                {
                    // Use raw SQL query to avoid EF issues
                    var tickets = db.Database.SqlQuery<TicketInfo>(
                        "SELECT ID_Ve, ChoNgoi, QRCodePath FROM Ve WHERE ID_Ve IN ({0})",
                        string.Join(",", ticketIds)
                    ).ToList();
                    
                    ViewBag.Tickets = tickets;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading tickets: {ex.Message}");
                    ViewBag.Tickets = null;
                }
            }

            if (ViewBag.MovieName == null)
            {
                return RedirectToAction("BookingTicket");
            }

            // Clear session data
            Session.Remove("BookingSuccess_MovieName");
            Session.Remove("BookingSuccess_ShowDate");
            Session.Remove("BookingSuccess_ShowTime");
            Session.Remove("BookingSuccess_SeatNumbers");
            Session.Remove("BookingSuccess_TotalAmount");
            Session.Remove("BookingSuccess_RoomName");
            Session.Remove("BookingSuccess_Combos");
            Session.Remove("BookingSuccess_TicketIds");

            return View();
        }

        // Add this class to your Models or ViewModels folder
        public class TicketInfo
        {
            public int ID_Ve { get; set; }
            public string ChoNgoi { get; set; }
            public string QRCodePath { get; set; }
        }
    }
}