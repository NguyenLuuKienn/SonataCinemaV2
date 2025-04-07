using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using SonataCinemaV2.Models;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.SqlServer;

public class GeminiHelper
{
    private static readonly string API_KEY = "AIzaSyBBi3XWo0f2sNCkE5IEzWmpNDbcQcgU8xw";
    private static readonly string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private readonly CinemaV3Entities db;

    public GeminiHelper()
    {
        db = new CinemaV3Entities();
    }

    public async Task<string> GetResponseFromGemini(string userMessage)
    {
        try
        {
            // Lấy context data từ database
            var context = await GetContextData();
            Debug.WriteLine($"Context Data: {context}");

            // Tạo prompt với context
            var prompt = CreatePrompt(userMessage, context);
            Debug.WriteLine($"Prompt: {prompt}");

            using (var client = new HttpClient())
            {
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                Debug.WriteLine($"Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var requestUrl = $"{API_URL}?key={API_KEY}";

                var response = await client.PostAsync(requestUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API Response: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API error: {response.StatusCode} - {responseString}");
                }

                dynamic responseData = JsonConvert.DeserializeObject(responseString);

                if (responseData.candidates != null && responseData.candidates.Count > 0)
                {
                    return responseData.candidates[0].content.parts[0].text;
                }
                else
                {
                    throw new Exception("No valid response from API");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetResponseFromGemini: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            return "Xin lỗi, tôi không thể trả lời lúc này. Vui lòng thử lại sau.";
        }
    }

    private string CreatePrompt(string userMessage, string context)
    {
        return $@"
Bạn là nhân viên tư vấn của rạp chiếu phim Sonata Cinema, tên là Sonata. Hãy trả lời dựa trên thông tin sau:

Thông tin rạp:
{context}

Câu hỏi: {userMessage}

Quy tắc trả lời:
1. Trả lời ngắn gọn, thân thiện như đang chat trực tiếp
2. Không liệt kê dài dòng, chỉ nêu thông tin quan trọng và liên quan đến câu hỏi
3. Nếu là chào hỏi, chỉ chào đơn giản và hỏi có thể giúp gì
4. Với lịch chiếu:
   - Chỉ nêu tên phim, giờ chiếu và số ghế trống
   - Nếu không hỏi ngày cụ thể thì chỉ trả lời lịch hôm nay
5. Với phim đang chiếu:
   - Chỉ giới thiệu 2-3 phim có điểm cao nhất
   - Nêu ngắn gọn: tên phim, thể loại, điểm đánh giá
6. Luôn kết thúc bằng câu ngắn gọn mời hỏi thêm
7. Trả lời các cuộc trò chuyện như một người thật, không phải bot
8. Nếu không trả lời được thì hãy yêu cầu liên lạc với nhân viên
Giọng điệu:
- Thân thiện, gần gũi
- Dùng 'mình', 'bạn' thay vì 'chúng tôi', 'quý khách'
- Tránh quá formal hoặc marketing
- Chat như người thật, không như bot

Trả lời:";
    }

    private async Task<string> GetContextData()
    {
        try
        {
            var today = DateTime.Today;
            var nextWeek = today.AddDays(7);

            // Lấy lịch chiếu từ hôm nay đến 7 ngày tới
            var lichChieuQuery = await db.LichChieux
                .Where(lc => DbFunctions.TruncateTime(lc.NgayChieu) >= today &&
                            DbFunctions.TruncateTime(lc.NgayChieu) <= nextWeek)
                .Select(lc => new
                {
                    lc.ID_LichChieu,
                    lc.NgayChieu,
                    lc.GioChieu,
                    PhimInfo = lc.Phim.TenPhim,
                    Phong = lc.PhongChieu.TenPhong,
                    GiaVe = lc.GiaVe,
                    ThoiLuong = lc.Phim.ThoiLuong
                })
                .ToListAsync();

            // Xử lý và format dữ liệu lịch chiếu
            var lichChieu = lichChieuQuery
                .GroupBy(lc => lc.NgayChieu.Date)
                .Select(group => new
                {
                    Ngay = group.Key.ToString("dd/MM/yyyy"),
                    SuatChieu = group.Select(lc => new
                    {
                        PhimInfo = lc.PhimInfo,
                        Phong = lc.Phong,
                        // Sửa lại cách format giờ chiếu
                        GioChieu = lc.GioChieu.ToString(@"hh\:mm"),
                        GiaVe = String.Format("{0:N0}đ", lc.GiaVe),
                        ThoiLuong = $"{lc.ThoiLuong} phút",
                        GheConLai = db.Ghes.Count(g => !db.Ves.Any(v =>
                            v.ID_LichChieu == lc.ID_LichChieu &&
                            v.ChoNgoi == g.TenGhe))
                    })
                .OrderBy(sc => sc.GioChieu)
                .ToList()
                })
                .OrderBy(x => DateTime.ParseExact(x.Ngay, "dd/MM/yyyy", null))
                .ToList();

            // Lấy thông tin phim đang chiếu
            var phimDangChieu = await db.Phims
                .Where(p => p.TrangThai == "Đang chiếu")
                .Select(p => new
                {
                    TenPhim = p.TenPhim,
                    TheLoai = p.TheLoai,
                    ThoiLuong = p.ThoiLuong,
                    DaoDien = p.DaoDien,
                    DienVien = p.DienVien,
                    MoTa = p.MoTa,
                    DiemTB = Math.Round(p.DanhGias.Average(d => d.DiemDanhGia) ?? 0, 1),
                    LuotDanhGia = p.DanhGias.Count()
                })
                .OrderByDescending(p => p.DiemTB)
                .ToListAsync();

           

            var context = new
            {
                LichChieu = lichChieu,
                PhimDangChieu = phimDangChieu,
                NgayCapNhat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            var jsonContext = JsonConvert.SerializeObject(context, Formatting.Indented);
            Debug.WriteLine($"Context Data Generated: {jsonContext}");
            return jsonContext;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetContextData: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
    }
}