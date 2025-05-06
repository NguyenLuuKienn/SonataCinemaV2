using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.ViewModel;
using System.Web.Security;
using System.IO;
using System.Drawing;
using System.Data.Entity.Validation;
using SonataCinemaV2.Models;
using SonataCinemaV2.Quyen;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.ComponentModel;
using OfficeOpenXml;
using System.Globalization;

namespace SonataCinema.Controllers
{
    [AuthorizeRoles]
    public class AdminController : Controller
    {
        // GET: Admin
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult IndexAdmin()
        {
            return View();
        }
        // phần toltal
        public ActionResult TotalPartial()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var firstDayofMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayofMonth = firstDayofMonth.AddMonths(1);

            // doanh thu theo ngày
            ViewBag.DoanhThuHomNay = db.Ves
            .Where(v => v.NgayDat >= today && v.NgayDat < tomorrow && v.TrangThai == "Thành Công")
            .Sum(v => (decimal?)v.Gia) ?? 0M;



            // theo tháng
            ViewBag.DoanhThuThang = db.Ves
            .Where(v => v.NgayDat >= firstDayofMonth && v.NgayDat < lastDayofMonth && v.TrangThai == "Thành Công")
            .Sum(v => (decimal?)v.Gia) ?? 0M;

            // vé
            ViewBag.TongVe = db.Ves.Count();
            ViewBag.VeDaBan = db.Ves.Count(v => v.TrangThai == "Thành Công");
            ViewBag.VeDaHuy = db.Ves.Count(v => v.TrangThai == "Đã huỷ");

            // phim
            ViewBag.PhimDangChieu = db.Phims.Count(p => p.TrangThai == "Đang chiếu");
            ViewBag.PhimSapChieu = db.Phims.Count(p => p.TrangThai == "Sắp chiếu");

            // top phim trong tháng
            var topPhim = db.Ves
            .Where(v => v.NgayDat >= firstDayofMonth &&
                        v.NgayDat < lastDayofMonth &&
                        v.TrangThai == "Thành công")
            .GroupBy(v => new {
                PhimId = v.LichChieu.ID_Phim,
                TenPhim = v.LichChieu.Phim.TenPhim
            })
            .Select(g => new TopPhimViewModel
            {
                Phim = g.Key.TenPhim,
                SoVe = g.Count(),
                DoanhThu = g.Sum(v => v.Gia)
            })
            .OrderByDescending(x => x.DoanhThu)
            .Take(5)
            .ToList();

            ViewBag.TopPhim = topPhim;
            return PartialView("TotalPartial");
        }
        // biểu đồ
        [HttpGet]
        public JsonResult GetDoanhThuTheoNgay(int thang, int nam)
        {
            try
            {
                var firstDayOfMonth = new DateTime(nam, thang, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1);

                var doanhThuTheoNgay = db.Ves
                    .Where(v => v.NgayDat >= firstDayOfMonth
                            && v.NgayDat < lastDayOfMonth
                            && v.TrangThai == "Thành Công")
                    .GroupBy(v => EntityFunctions.TruncateTime(v.NgayDat))
                    .Select(g => new
                    {
                        Ngay = g.Key,
                        DoanhThu = g.Sum(v => (decimal?)v.Gia) ?? 0M
                    })
                    .OrderBy(x => x.Ngay)
                    .ToList()
                    .Select(x => new
                    {
                        Ngay = x.Ngay.Value.ToString("dd/MM/yyyy"),
                        DoanhThu = x.DoanhThu
                    });

                // Debug log
                foreach (var item in doanhThuTheoNgay)
                {
                    System.Diagnostics.Debug.WriteLine($"Ngày: {item.Ngay}, Doanh thu: {item.DoanhThu}");
                }

                return Json(doanhThuTheoNgay, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetDoanhThuTheoNgay: {ex.Message}");
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ExportExcelPartial()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                // Thống kê theo phim
                var movieStats = (from v in db.Ves
                                  join lc in db.LichChieux on v.ID_LichChieu equals lc.ID_LichChieu
                                  join p in db.Phims on lc.ID_Phim equals p.ID_Phim
                                  where v.TrangThai == "Thành Công"
                                  group v by new { p.ID_Phim, p.TenPhim } into g
                                  select new MovieStatsViewModel
                                  {
                                      TenPhim = g.Key.TenPhim,
                                      SoVe = g.Count(),
                                      DoanhThu = g.Sum(x => x.Gia)
                                  })
                                 .OrderByDescending(x => x.DoanhThu)
                                 .ToList();

                // Thống kê theo phòng chiếu
                var roomStats = (from v in db.Ves
                                 join lc in db.LichChieux on v.ID_LichChieu equals lc.ID_LichChieu
                                 join pc in db.PhongChieux on lc.ID_Phong equals pc.ID_Phong
                                 where v.TrangThai == "Thành Công"
                                 group v by new { pc.ID_Phong, pc.TenPhong } into g
                                 select new RoomStatsViewModel
                                 {
                                     TenPhong = g.Key.TenPhong,
                                     SoSuatChieu = db.LichChieux.Count(x => x.ID_Phong == g.Key.ID_Phong),
                                     DoanhThu = g.Sum(x => x.Gia)
                                 })
                                .OrderByDescending(x => x.DoanhThu)
                                .ToList();

                ViewBag.MovieStats = movieStats;
                ViewBag.RoomStats = roomStats;

                // Debug log để kiểm tra dữ liệu
                System.Diagnostics.Debug.WriteLine($"Số lượng phim: {ViewBag.MovieStats.Count}");
                foreach (var item in ViewBag.MovieStats)
                {
                    System.Diagnostics.Debug.WriteLine($"Phim: {item.TenPhim}, Số vé: {item.SoVe}, Doanh thu: {item.DoanhThu}");
                }

                System.Diagnostics.Debug.WriteLine($"Số lượng phòng: {ViewBag.RoomStats.Count}");
                foreach (var item in ViewBag.RoomStats)
                {
                    System.Diagnostics.Debug.WriteLine($"Phòng: {item.TenPhong}, Suất chiếu: {item.SoSuatChieu}, Doanh thu: {item.DoanhThu}");
                }

                // Doanh thu hôm nay - Sử dụng DbFunctions.TruncateTime
                var doanhThuHomNay = db.Ves
                    .Where(v => DbFunctions.TruncateTime(v.NgayDat) == DbFunctions.TruncateTime(today)
                           && v.TrangThai == "Thành Công")
                    .Sum(v => (decimal?)v.Gia) ?? 0;
                ViewBag.DoanhThuHomNay = doanhThuHomNay;

                // Doanh thu tuần này
                var doanhThuTuan = db.Ves
                    .Where(v => v.NgayDat >= startOfWeek && v.NgayDat < tomorrow
                           && v.TrangThai == "Thành Công")
                    .Sum(v => (decimal?)v.Gia) ?? 0;
                ViewBag.DoanhThuTuan = doanhThuTuan;

                // Doanh thu tháng này
                var doanhThuThang = db.Ves
                    .Where(v => v.NgayDat >= startOfMonth && v.NgayDat < endOfMonth
                           && v.TrangThai == "Thành Công")
                    .Sum(v => (decimal?)v.Gia) ?? 0;
                ViewBag.DoanhThuThang = doanhThuThang;

                // Số vé hôm nay
                var veHomNay = db.Ves
                    .Count(v => DbFunctions.TruncateTime(v.NgayDat) == DbFunctions.TruncateTime(today)
                           && v.TrangThai == "Thành Công");
                ViewBag.VeHomNay = veHomNay;

                // Số vé tuần này
                var veTuan = db.Ves
                    .Count(v => v.NgayDat >= startOfWeek && v.NgayDat < tomorrow
                           && v.TrangThai == "Thành Công");
                ViewBag.VeTuan = veTuan;

                // Số vé tháng này
                var veThang = db.Ves
                    .Count(v => v.NgayDat >= startOfMonth && v.NgayDat < endOfMonth
                           && v.TrangThai == "Thành Công");
                ViewBag.VeThang = veThang;

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Today: {today}");
                System.Diagnostics.Debug.WriteLine($"StartOfWeek: {startOfWeek}");
                System.Diagnostics.Debug.WriteLine($"StartOfMonth: {startOfMonth}");
                System.Diagnostics.Debug.WriteLine($"Doanh thu hôm nay: {doanhThuHomNay}");
                System.Diagnostics.Debug.WriteLine($"Số vé hôm nay: {veHomNay}");

                // Danh sách phim và phòng cho dropdown
                ViewBag.DanhSachPhim = new SelectList(db.Phims, "ID_Phim", "TenPhim");
                ViewBag.DanhSachPhong = new SelectList(db.PhongChieux, "ID_Phong", "TenPhong");

                return PartialView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExportExcelPartial: {ex.Message}");
                return Content("Có lỗi xảy ra khi tải dữ liệu thống kê: " + ex.Message);
            }
        }

        public ActionResult ExportExcel(string loaiThongKe, string khoangThoiGian,
    DateTime? startDate, DateTime? endDate, string weekInput, string monthInput,
    int? yearInput, int? ID_Phim, int? ID_Phong)
        {
            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Báo cáo");

                    // Xác định khoảng thời gian
                    DateTime fromDate, toDate;
                    switch (khoangThoiGian)
                    {
                        case "Ngay" when startDate.HasValue && endDate.HasValue:
                            fromDate = startDate.Value.Date;
                            toDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                            break;
                        case "Tuan" when !string.IsNullOrEmpty(weekInput):
                            var weekParts = weekInput.Split('-');
                            int year = int.Parse(weekParts[0]);
                            int weekNumber = int.Parse(weekParts[1].Replace("W", ""));
                            var jan1 = new DateTime(year, 1, 1);
                            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
                            var firstThursday = jan1.AddDays(daysOffset);
                            var cal = CultureInfo.CurrentCulture.Calendar;
                            var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                            var weekNum = weekNumber;
                            if (firstWeek <= 1) weekNum -= 1;
                            fromDate = firstThursday.AddDays(weekNum * 7 - 3);
                            toDate = fromDate.AddDays(7).AddSeconds(-1);
                            break;
                        case "Thang" when !string.IsNullOrEmpty(monthInput):
                            var monthParts = monthInput.Split('-');
                            fromDate = new DateTime(int.Parse(monthParts[0]), int.Parse(monthParts[1]), 1);
                            toDate = fromDate.AddMonths(1).AddSeconds(-1);
                            break;
                        case "Nam" when yearInput.HasValue:
                            fromDate = new DateTime(yearInput.Value, 1, 1);
                            toDate = fromDate.AddYears(1).AddSeconds(-1);
                            break;
                        default:
                            fromDate = DateTime.Now.Date;
                            toDate = fromDate.AddDays(1).AddSeconds(-1);
                            break;
                    }

                    worksheet.Cells["A1:D1"].Merge = true;
                    worksheet.Cells["A1"].Value = "STT";
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.Font.Size = 16;
                    worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A2:D2"].Merge = true;
                    string reportTitle = "BÁO CÁO " + (loaiThongKe == "Phim" ? "DOANH THU THEO PHIM" :
                                                      loaiThongKe == "PhongChieu" ? "DOANH THU THEO PHÒNG CHIẾU" :
                                                      loaiThongKe == "Blog" ? "THỐNG KÊ BLOG" : "DOANH THU TOÀN RẠP");
                    worksheet.Cells["A2"].Value = reportTitle;
                    worksheet.Cells["A2"].Style.Font.Bold = true;
                    worksheet.Cells["A2"].Style.Font.Size = 14;
                    worksheet.Cells["A2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A3:D3"].Merge = true;
                    worksheet.Cells["A3"].Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    worksheet.Cells["A3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A4:D4"].Merge = true;
                    worksheet.Cells["A4"].Value = $"Người xuất báo cáo: {User.Identity.Name}";
                    worksheet.Cells["A4"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A5:D5"].Merge = true;
                    worksheet.Cells["A5"].Value = $"Khoảng thời gian: Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}";
                    worksheet.Cells["A5"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A6:D6"].Merge = true;

                    worksheet.Cells["A7"].Value = "STT";
                    worksheet.Cells["B7"].Value = loaiThongKe == "Blog" ? "Tiêu đề Blog" : "Tên";
                    worksheet.Cells["C7"].Value = loaiThongKe == "Blog" ? "Người đăng" : "Thời gian";
                    worksheet.Cells["D7"].Value = loaiThongKe == "Blog" ? "Lượt xem" : "Doanh Thu";

                    var headerRange = worksheet.Cells["A7:D7"];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    headerRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    int row = 8;
                    if (loaiThongKe == "Blog")
                    {
                        var blogData = db.Blogs
                            .Where(b => b.NgayDang >= fromDate && b.NgayDang <= toDate)
                            .Select(b => new
                            {
                                TieuDe = b.TieuDe,
                                NguoiDang = b.NhanVien.TenNhanVien,
                                LuotXem = b.LuotXem
                            })
                            .OrderByDescending(b => b.LuotXem)
                            .ToList();

                        foreach (var item in blogData)
                        {
                            worksheet.Cells[row, 1].Value = row - 7;
                            worksheet.Cells[row, 2].Value = item.TieuDe;
                            worksheet.Cells[row, 3].Value = item.NguoiDang;
                            worksheet.Cells[row, 4].Value = item.LuotXem;
                            row++;
                        }
                    }
                    else
                    {
                        var query = db.Ves.Where(v => v.NgayDat >= fromDate &&
                                                    v.NgayDat <= toDate &&
                                                    v.TrangThai == "Thành Công");

                        if (loaiThongKe == "Phim" && ID_Phim.HasValue)
                        {
                            query = query.Where(v => v.LichChieu.ID_Phim == ID_Phim);
                        }
                        else if (loaiThongKe == "PhongChieu" && ID_Phong.HasValue)
                        {
                            query = query.Where(v => v.LichChieu.ID_Phong == ID_Phong);
                        }

                        var data = query.GroupBy(v => new {
                            Ten = loaiThongKe == "Phim" ? v.LichChieu.Phim.TenPhim :
                                      loaiThongKe == "PhongChieu" ? v.LichChieu.PhongChieu.TenPhong :
                                      "Toàn Rạp",
                            Ngay = EntityFunctions.TruncateTime(v.NgayDat)
                        })
                            .Select(g => new {
                                Ten = g.Key.Ten,
                                Ngay = g.Key.Ngay,
                                DoanhThu = g.Sum(v => v.Gia)
                            })
                            .OrderBy(x => x.Ngay)
                            .ToList();

                        foreach (var item in data)
                        {
                            worksheet.Cells[row, 1].Value = row - 7;
                            worksheet.Cells[row, 2].Value = item.Ten;
                            worksheet.Cells[row, 3].Value = item.Ngay.Value.ToString("dd/MM/yyyy");
                            worksheet.Cells[row, 4].Value = item.DoanhThu;

                            worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                            row++;
                        }

                        worksheet.Cells[row, 1].Value = "";
                        worksheet.Cells[row, 2].Value = "Tổng cộng:";
                        worksheet.Cells[row, 2].Style.Font.Bold = true;
                        worksheet.Cells[row, 3].Value = "";
                        worksheet.Cells[row, 4].Formula = $"SUM(D8:D{row - 1})";
                        worksheet.Cells[row, 4].Style.Font.Bold = true;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    }

                    var dataRange = worksheet.Cells[8, 1, row, 4];
                    dataRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    worksheet.Column(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Column(3).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"BaoCao_{loaiThongKe}_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExportExcel: {ex.Message}");
                return Content("Có lỗi xảy ra khi xuất báo cáo: " + ex.Message);
            }
        }

        public ActionResult DangXuat()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
