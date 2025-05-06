using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;
using SonataCinemaV2.Quyen;

namespace SonataCinemaV2.Controllers
{
    public class BlogController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();
        // GET: Blog
        public ActionResult BlogPage(string category = "", string search = "")
        {
            var blogs = db.Blogs
                .Include(b => b.NhanVien)
                .Where(b => b.TrangThai == true);

            if (!string.IsNullOrEmpty(category))
            {
                blogs = blogs.Where(b => b.TheLoai == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                blogs = blogs.Where(b => b.TieuDe.Contains(search) ||
                                       b.NoiDung.Contains(search));
            }

            return View(blogs.OrderByDescending(b => b.NgayDang).ToList());
        }
        public ActionResult ChiTiet(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var blog = db.Blogs
                .Include(b => b.NhanVien)
                .FirstOrDefault(b => b.ID_Blog == id);

            if (blog == null)
            {
                return HttpNotFound();
            }

            blog.LuotXem++;

            var relatedPosts = db.Blogs
            .Where(b => b.ID_Blog != blog.ID_Blog
                   && b.TheLoai == blog.TheLoai
                   && b.TrangThai == true)
            .OrderByDescending(b => b.NgayDang)
            .Take(3)
            .ToList();

            ViewBag.RelatedPosts = relatedPosts;
            db.SaveChanges();

            return View(blog);
        }
        public ActionResult QuanLyBlogPartial()
        {
            var blogs = db.Blogs
                .Include("NhanVien")
                .OrderByDescending(b => b.NgayDang)
                .ToList();
            return PartialView("_QuanLyBlogPartial", blogs);
        }

        [AuthorizeRoles]
        [ValidateInput(false)]
        [HttpPost]
        public JsonResult Create(Blog blog, HttpPostedFileBase HinhAnh)
        {
            try
            {

                dynamic nhanVienSession = Session["NhanVien"];
                if (nhanVienSession == null)
                {
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại!" });
                }

                int nhanVienId = nhanVienSession.ID_NhanVien;

                blog.ID_NhanVien = nhanVienId;
                blog.NgayDang = DateTime.Now;
                blog.TrangThai = true;
                blog.LuotXem = 0;

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    var fileName = $"blog_{DateTime.Now.Ticks}{Path.GetExtension(HinhAnh.FileName)}";
                    var path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                    HinhAnh.SaveAs(path);
                    blog.HinhAnh = "/Content/img/" + fileName;
                }

                try
                {
                    db.Blogs.Add(blog);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Thêm bài viết thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi lưu bài viết: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi không xác định: " + ex.Message });
            }       
        }
        [AuthorizeRoles]
        [ValidateInput(false)]
        [HttpPost]
        public JsonResult Edit(Blog blog, HttpPostedFileBase HinhAnh)
        {
            try
            {
                var existingBlog = db.Blogs.Find(blog.ID_Blog);
                if (existingBlog == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài viết!" });
                }

                existingBlog.TieuDe = blog.TieuDe;
                existingBlog.NoiDung = blog.NoiDung;
                existingBlog.TheLoai = blog.TheLoai;

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = $"blog_{DateTime.Now.Ticks}{Path.GetExtension(HinhAnh.FileName)}";
                    string path = Path.Combine(Server.MapPath("~/Content/img/"), fileName);
                    HinhAnh.SaveAs(path);
                    existingBlog.HinhAnh = "/Content/img/" + fileName;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {

                var blog = db.Blogs.Find(id);
                if (blog == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài viết!" });
                }

                db.Blogs.Remove(blog);
                db.SaveChanges();

                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [AuthorizeRoles]
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            try
            {
                var blog = db.Blogs.Find(id);
                if (blog == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy blog" });
                }
                dynamic nhanVienSession = Session["NhanVien"];
                if (nhanVienSession == null)
                {
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn" });
                }

                int currentUserId = nhanVienSession.ID_NhanVien;
                bool isAdmin = Session["Admin"] != null && (bool)Session["Admin"];

                if (!isAdmin && blog.ID_NhanVien != currentUserId)
                {
                    return Json(new { success = false, message = "Không có quyền thay đổi trạng thái" });
                }

                blog.TrangThai = !blog.TrangThai;
                db.SaveChanges();
                return Json(new { success = true, newStatus = blog.TrangThai });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetBlogList()
        {
            try
            {
                var blogs = db.Blogs
                    .Include(b => b.NhanVien)
                    .Select(b => new
                    {
                        id = b.ID_Blog,
                        hinhAnh = b.HinhAnh,
                        tieuDe = b.TieuDe,
                        theLoai = b.TheLoai,
                        nguoiViet = b.NhanVien.TenNhanVien,
                        ngayDang = b.NgayDang,
                        trangThai = b.TrangThai,
                        luotXem = b.LuotXem,
                        authorId = b.ID_NhanVien
                    })
                    .OrderByDescending(b => b.ngayDang)
                    .ToList();

                return Json(new { success = true, data = blogs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetBlogById(int id)
        {
            try
            {
                var blog = db.Blogs.Find(id);
                if (blog == null)
                    return Json(new { success = false, message = "Không tìm thấy blog" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ID_Blog = blog.ID_Blog,
                        TieuDe = blog.TieuDe,
                        NoiDung = blog.NoiDung,
                        TheLoai = blog.TheLoai,
                        HinhAnh = blog.HinhAnh,
                        TrangThai = blog.TrangThai,
                        ID_NhanVien = blog.ID_NhanVien
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
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