using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.Helper
{
    public class SimpleBotHelper
    {
        private static readonly Dictionary<string, string[]> keywords = new Dictionary<string, string[]>
    {
        { "chào", new[] { "hi", "hello", "chào", "xin chào" } },
        { "giá vé", new[] { "giá", "vé", "tiền", "bao nhiêu" } },
        { "lịch chiếu", new[] { "lịch", "chiếu", "suất chiếu", "giờ chiếu", "phim gì", "lịch chiếu" } },
        { "đặt vé", new[] { "đặt", "mua", "book", "đăng ký" } },
        { "khuyến mãi", new[] { "khuyến mãi", "giảm giá", "ưu đãi", "voucher" } },
        { "thành viên", new[] { "thành viên", "member", "thẻ", "điểm" } },
        { "địa chỉ", new[] { "địa chỉ", "ở đâu", "chỗ nào", "địa điểm" } },
        { "liên hệ", new[] { "liên hệ", "hotline", "số điện thoại", "email" } },
        { "bắp nước", new[] { "bắp", "nước", "đồ ăn", "combo" } },
        { "phòng chiếu", new[] { "phòng", "rạp", "màn hình", "âm thanh" } }
    };

        private static readonly Dictionary<string, string> responses = new Dictionary<string, string>
    {
        { "chào", @"Xin chào! Tôi là trợ lý ảo của Sonata Cinema. 
        Tôi có thể giúp bạn:
        - Xem giá vé và lịch chiếu
        - Đặt vé xem phim
        - Tư vấn chương trình khuyến mãi
        - Thông tin thành viên
        Bạn cần hỗ trợ gì ạ?" },

                { "giá vé", @"🎫 Giá vé tại Sonata Cinema:
        1. Phim 2D:
           - Thứ 2 đến thứ 5: 45,000đ
           - Thứ 6 đến Chủ nhật: 60,000đ
        2. Phim 3D:
           - Thứ 2 đến thứ 5: 60,000đ
           - Thứ 6 đến Chủ nhật: 95,000đ
        * Giá đã bao gồm kính 3D (nếu có)
        * Giá vé có thể thay đổi vào dịp Lễ/Tết" },

                { "lịch chiếu", @"🎬 Để xem lịch chiếu phim:
        1. Truy cập website: www.sonatacinema.com/schedule
        2. Tải ứng dụng Sonata Cinema trên điện thoại
        3. Gọi hotline: 1900 xxxx

        Bạn muốn xem phim gì? Tôi có thể kiểm tra lịch chiếu cụ thể giúp bạn." },

                { "đặt vé", @"🎟️ Để đặt vé xem phim, bạn có thể:
        1. Đặt online:
           - Website: www.sonatacinema.com
           - Ứng dụng Sonata Cinema
        2. Đặt trực tiếp:
           - Tại quầy vé của rạp
           - Gọi hotline: 1900 xxxx
        * Ưu đãi đặt vé online: Giảm 10% giá vé" },

                { "khuyến mãi", @"🎉 Chương trình khuyến mãi hiện có:
        1. Happy Monday: Giảm 20% giá vé thứ 2
        2. Ladies Wednesday: Giảm 15% cho khách nữ
        3. Student Friday: Giảm 20% cho học sinh/sinh viên
        4. Combo bắp nước: Giảm 15% khi mua vé
        5. Ưu đãi sinh nhật: Tặng 1 vé xem phim

        * Vui lòng mang theo giấy tờ để hưởng ưu đãi" },

                { "thành viên", @"💳 Đặc quyền thành viên Sonata Cinema:
        1. Tích điểm đổi vé miễn phí
        2. Giảm 10% giá vé
        3. Ưu đãi bắp nước
        4. Quà tặng sinh nhật
        5. Vé suất chiếu sớm

        Đăng ký thành viên tại: www.sonatacinema.com/member" },

                { "địa chỉ", @"📍 Sonata Cinema:
        - Địa chỉ: 123 Đường Này, Ngõ Nọ, Chỗ Kia
        - Gần ngã tư đó đó
        - Có bãi giữ xe rất rộng nhưng lúc nào cũng kín chỗ
        - Mở cửa: 8:00 - 24:00 hàng ngày" },

                { "liên hệ", @"📞 Thông tin liên hệ:
        - Hotline: 0947483487 (8:00 - 22:00)
        - Email: nguyenluukienn@gmail.com
        - Facebook: fb.com/nlk.rufus
        - Instagram: @sonatacinema
        - Website: www.sonatacinema.com" },

                { "bắp nước", @"🍿 Menu bắp nước:
        1. Combo 1 (89k): 1 bắp + 1 nước
        2. Combo 2 (119k): 1 bắp + 2 nước
        3. Combo 3 (149k): 2 bắp + 2 nước
        * Miễn phí refill bắp rang cho Combo 2,3
        * Giảm 10% cho thành viên" },

                { "phòng chiếu", @"🎥 Công nghệ phòng chiếu:
        - Màn hình 4K sắc nét (chắc vậy)
        - Âm thanh Dolby 7.1
        - Ghế bọc da cao cấp
        - Máy chiếu Laser hiện đại
        - Phòng chiếu được vệ sinh sau mỗi suất" }
            };

        public static string GetResponse(string input)
        {
            input = input.ToLower().Trim();

            // Tìm từ khóa phù hợp
            foreach (var category in keywords)
            {
                if (category.Value.Any(keyword => input.Contains(keyword)))
                {
                    return responses[category.Key];
                }
            }

            // Câu trả lời mặc định
            return @"Xin lỗi, tôi không hiểu câu hỏi của bạn. 
            Bạn có thể hỏi về:
            - Giá vé xem phim 🎫
            - Lịch chiếu phim 🎬
            - Cách đặt vé 🎟️
            - Chương trình khuyến mãi 🎉
            - Thông tin thành viên 💳
            - Menu bắp nước 🍿
            - Địa chỉ và liên hệ 📍";
        }
    }
}