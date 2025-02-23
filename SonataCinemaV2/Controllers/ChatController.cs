using SonataCinemaV2.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Quyen;
using SonataCinemaV2.Models;
using System.Net;


namespace SonataCinemaV2.Controllers
{
    public class ChatController : Controller
    {
        private CinemaV3Entities db = new CinemaV3Entities();
        [HttpGet]
        public ActionResult ChatIndex()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = db.KhachHangs.FirstOrDefault(k => k.Email == User.Identity.Name);
                ViewBag.CustomerName = user?.TenKhachHang ?? User.Identity.Name;
            }
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            try
            {
                // Tạo session ID từ cookie hoặc tạo mới
                string sessionId = Request.Cookies["chat_session_id"]?.Value;
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString();
                    var cookie = new HttpCookie("chat_session_id", sessionId)
                    {
                        Expires = DateTime.Now.AddDays(1)
                    };
                    Response.Cookies.Add(cookie);
                }

                var response = await DialogflowHelper.GetResponseFromDialogflow(message, sessionId);
                return Json(new { success = true, message = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        [AuthorizeRoles]
        public ActionResult HelperCustomer()
        {
            var userEmail = User.Identity.Name;
            var nhanVien = db.NhanViens.FirstOrDefault(n => n.Email == userEmail);

            if (nhanVien == null || (nhanVien.QuyenHan != "Admin" && nhanVien.QuyenHan != "Staff"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            ViewBag.StaffName = nhanVien.TenNhanVien;
            return PartialView("_HelperCustomer");
        }
        [HttpGet]
        public JsonResult GetChatHistory(int customerId)
        {
            var messages = db.ChatMessages
                .Where(m => m.ID_KhachHang == customerId && m.Status == "Active")
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    m.ID_Chat,
                    m.Message,
                    m.Role,
                    Sender = m.Role == "Customer" ? m.KhachHang.TenKhachHang : m.NhanVien.TenNhanVien,
                    m.Timestamp
                })
                .ToList();

            return Json(messages, JsonRequestBehavior.AllowGet);
        }


    }
}