using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Quyen;

namespace SonataCinemaV2.Controllers
{
    public class ComboController : Controller
    {
        private readonly CinemaV3Entities db = new CinemaV3Entities();
        // GET: Combo
        public ActionResult ComboPage()
        {
            return View();
        }
        public ActionResult QuanLyComboPartial()
        {
            var combos = db.Combos.OrderByDescending(c => c.ID_Combo).ToList();
            return PartialView("_QuanLyComboPartial", combos);
        }

        [HttpGet]
        public JsonResult GetComboList()
        {
            try
            {
                var combos = db.Combos
                    .Select(c => new
                    {
                        id = c.ID_Combo,
                        tenCombo = c.TenCombo,
                        moTa = c.MoTa,
                        gia = c.Gia,
                        hinhAnh = c.HinhAnh,
                        trangThai = c.TrangThai
                    })
                    .OrderByDescending(c => c.id)
                    .ToList();

                return Json(new { success = true, data = combos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetComboById(int id)
        {
            try
            {
                var combo = db.Combos.Find(id);
                if (combo == null)
                    return Json(new { success = false, message = "Không tìm thấy combo" }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, data = combo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Combo combo, HttpPostedFileBase HinhAnh)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    try
                    {
                        string fileName = $"combo_{DateTime.Now.Ticks}{Path.GetExtension(HinhAnh.FileName)}";
                        string folderPath = Server.MapPath("~/Content/images/combos");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string filePath = Path.Combine(folderPath, fileName);
                        HinhAnh.SaveAs(filePath);
                        combo.HinhAnh = "/Content/images/combos/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Lỗi khi lưu hình ảnh: " + ex.Message });
                    }
                }

                combo.TrangThai = true;

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Combos.Add(combo);
                        db.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = "Thêm combo thành công!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Lỗi khi lưu vào database: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Combo combo, HttpPostedFileBase HinhAnh)
        {
            try
            {
                var existingCombo = db.Combos.Find(combo.ID_Combo);
                if (existingCombo == null)
                    return Json(new { success = false, message = "Không tìm thấy combo!" });

                existingCombo.TenCombo = combo.TenCombo;
                existingCombo.MoTa = combo.MoTa;
                existingCombo.Gia = combo.Gia;

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    var fileName = $"combo_{DateTime.Now.Ticks}{Path.GetExtension(HinhAnh.FileName)}";
                    var path = Path.Combine(Server.MapPath("~/Content/images/combos"), fileName);
                    HinhAnh.SaveAs(path);
                    existingCombo.HinhAnh = "/Content/images/combos/" + fileName;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật combo thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            try
            {
                var combo = db.Combos.Find(id);
                if (combo == null)
                    return Json(new { success = false, message = "Không tìm thấy combo!" });

                combo.TrangThai = false; 
                db.SaveChanges();
                return Json(new { success = true, message = "Xóa combo thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            try
            {
                var combo = db.Combos.Find(id);
                if (combo == null)
                    return Json(new { success = false, message = "Không tìm thấy combo" });

                combo.TrangThai = !combo.TrangThai;
                db.SaveChanges();
                return Json(new { success = true, newStatus = combo.TrangThai });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}