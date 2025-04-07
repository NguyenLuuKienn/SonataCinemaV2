using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Text;
using SonataCinemaV2.Models;

namespace SonataCinemaV2.Helper
{
    public class EmailHelper
    {
        private static readonly string SmtpHost = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;
        private static readonly string FromEmail = "nguyenluukien.d23ctc4@muce.edu.vn"; // Email của rạp
        private static readonly string Password = "uabjenvfrctxmjox"; // App password từ Google
        private static readonly string FromName = "Sonata Cinema";

        public static async Task SendBookingConfirmationEmail(string toEmail, string customerName, string movieName, string showTime, string seats,string combos, decimal totalAmount)
        {
            var comboInfo = string.IsNullOrEmpty(combos) ? "Không có combo" : combos;
            string subject = "Xác nhận đặt vé - Sonata Cinema";
            string body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #0061f2;'>Xác nhận đặt vé thành công</h2>
                <p>Kính gửi {customerName},</p>
                <p>Cảm ơn bạn đã đặt vé tại Sonata Cinema. Dưới đây là thông tin vé của bạn:</p>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Phim:</strong> {movieName}</p>
                    <p><strong>Suất chiếu:</strong> {showTime}</p>
                    <p><strong>Ghế:</strong> {seats}</p>
                    <p><strong>Combo đã chọn:</strong> {comboInfo}</p>
                    <div style='margin-top: 15px; border-top: 1px solid #ddd; padding-top: 15px;'>
                    <p style='font-size: 1.2em; color: #e74c3c;'><strong>Tổng tiền:</strong> {totalAmount:N0} VNĐ</p>
                </div>
                    <p><strong>Điểm thưởng:</strong> +1</p>
                </div>

                <p style='background: #fff3cd; padding: 10px; border-radius: 5px;'>
                    ⚠️ Vui lòng đến rạp trước giờ chiếu 15-30 phút để check-in.
                </p>

                <p style='color: #0061f2;'>Chúc bạn xem phim vui vẻ! 🎬</p>
                
                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                    <p style='color: #666; font-size: 12px;'>Lưu ý:</p>
                    <ul style='color: #666; font-size: 12px;'>
                        <li>Đây là email tự động, vui lòng không trả lời.</li>
                        <li>Nếu cần hỗ trợ, vui lòng liên hệ hotline: 0947483487</li>
                        <li>123 Đường Này, Ngõ Nọ, Chỗ Kia</li>
                    </ul>
                </div>
            </div>
        </body>
        </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public static async Task SendCancellationEmail(string toEmail, string customerName, string movieName, string showTime, string seats, decimal refundAmount)
        {
            string subject = "Xác nhận huỷ vé - Sonata Cinema";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #0061f2;'>Xác nhận huỷ vé thành công</h2>
                        <p>Kính gửi {customerName},</p>
                        <p>Chúng tôi xác nhận đã huỷ vé của bạn với thông tin như sau:</p>
                        
                        <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Phim:</strong> {movieName}</p>
                            <p><strong>Suất chiếu:</strong> {showTime}</p>
                            <p><strong>Ghế:</strong> {seats}</p>
                            <p><strong>Số tiền hoàn trả:</strong> {refundAmount:N0} VNĐ (80% giá vé)</p>
                        </div>

                        <p>Số tiền sẽ được hoàn về tài khoản của bạn trong vòng 3-5 ngày làm việc.</p>
                        
                        <div style='margin-top: 30px; font-size: 12px; color: #666;'>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                            <p>Nếu cần hỗ trợ, vui lòng liên hệ hotline: 0947483487</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var client = new SmtpClient(SmtpHost, SmtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(FromEmail, Password);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(FromEmail, FromName);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    await client.SendMailAsync(message);
                }
            }
        }
        public static async Task SendResetPasswordEmail(string email, string resetLink)
        {
            string subject = "Đặt lại mật khẩu Sonata Cinema";
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>");
            body.AppendLine("<h1 style='color: #df9a2c;'>Sonata Cinema</h1>");
            body.AppendLine("</div>");
            body.AppendLine("<div style='padding: 20px; background-color: white;'>");
            body.AppendLine("<h2>Yêu cầu đặt lại mật khẩu</h2>");
            body.AppendLine("<p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng nhấp vào liên kết bên dưới để tiếp tục:</p>");
            body.AppendLine($"<p style='text-align: center; margin: 30px 0;'><a href='{resetLink}' style='background-color: #df9a2c; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đặt lại mật khẩu</a></p>");
            body.AppendLine("<p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>");
            body.AppendLine("<p>Lưu ý: Liên kết này sẽ hết hạn sau 24 giờ.</p>");
            body.AppendLine("</div>");
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 12px;'>");
            body.AppendLine("<p>© 2023 Sonata Cinema. Tất cả các quyền được bảo lưu.</p>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            await SendEmailAsync(email, subject, body.ToString());
        }
        public static async Task SendChatLogEmail(string toEmail, string customerName, List<ChatMessage> messages)
        {
            string subject = "Lịch sử cuộc trò chuyện - Sonata Cinema";

            var messageLog = new StringBuilder();
            foreach (var msg in messages)
            {
                messageLog.AppendLine($"{msg.Timestamp:HH:mm:ss} - {msg.Role}: {msg.Message}");
            }

            string body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2>Lịch sử cuộc trò chuyện</h2>
            <p>Xin chào {customerName},</p>
            <p>Dưới đây là nội dung cuộc trò chuyện của bạn với nhân viên hỗ trợ:</p>
            
            <div style='background: #f5f5f5; padding: 15px; border-radius: 5px;'>
                <pre style='white-space: pre-wrap;'>{messageLog}</pre>
            </div>
            
            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
        </body>
        </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}