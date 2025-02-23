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
            ViewBag.DanhSachPhim = new SelectList(db.Phims, "ID_Phim", "TenPhim");
            ViewBag.DanhSachPhong = new SelectList(db.PhongChieux, "ID_Phong", "TenPhong");
            return PartialView();
        }

        public ActionResult ExportExcel(string loaiThongKe, string khoangThoiGian,
    DateTime? startDate, DateTime? endDate, string weekInput, string monthInput,
    int? yearInput, int? ID_Phim, int? ID_Phong)
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

                        // Tính ngày đầu tuần
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

                // Thiết lập tiêu đề theo loại báo cáo
                if (loaiThongKe == "Blog")
                {
                    worksheet.Cells["A1:D1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Value = "STT";
                    worksheet.Cells["B1"].Value = "Tiêu đề Blog";
                    worksheet.Cells["C1"].Value = "Người đăng";
                    worksheet.Cells["D1"].Value = "Lượt xem";

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

                    int row = 2;
                    foreach (var item in blogData)
                    {
                        worksheet.Cells[row, 1].Value = row - 1;
                        worksheet.Cells[row, 2].Value = item.TieuDe;
                        worksheet.Cells[row, 3].Value = item.NguoiDang;
                        worksheet.Cells[row, 4].Value = item.LuotXem;
                        row++;
                    }
                }
                else
                {
                    worksheet.Cells["A1:D1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Value = "STT";
                    worksheet.Cells["B1"].Value = "Tên";
                    worksheet.Cells["C1"].Value = "Thời gian";
                    worksheet.Cells["D1"].Value = "Doanh Thu";

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

                    int row = 2;
                    foreach (var item in data)
                    {
                        worksheet.Cells[row, 1].Value = row - 1;
                        worksheet.Cells[row, 2].Value = item.Ten;
                        worksheet.Cells[row, 3].Value = item.Ngay.Value.ToString("dd/MM/yyyy");
                        worksheet.Cells[row, 4].Value = item.DoanhThu;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        row++;
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"BaoCao_{loaiThongKe}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult DangXuat()
        {
            //Test
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}