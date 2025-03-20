using Microsoft.ML.Data;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ML
{
    public class MovieRecommenderModel
    {
        public class MovieRatingData
        {
            [LoadColumn(0)]
            public string UserId { get; set; }  

            [LoadColumn(1)]
            public string MovieId { get; set; }  

            [LoadColumn(2)]
            public float Rating { get; set; }    

            [LoadColumn(3)]
            public DateTime Timestamp { get; set; }  
        }
        public class MovieRatingPrediction
        {
            public float Label { get; set; }
            public float Score { get; set; }
        }

        // Helper class để map dữ liệu
        public class RatingEntry
        {
            public string UserId { get; set; }
            public string MovieId { get; set; }
            public float Rating { get; set; }
            public DateTime Timestamp { get; set; }

            // Constructor để convert từ DanhGia entity
            public RatingEntry(DanhGia danhGia)
            {
                UserId = danhGia.ID_KhachHang.ToString();
                MovieId = danhGia.ID_Phim.ToString();
                Rating = (float)danhGia.DiemDanhGia;
                Timestamp = danhGia.ThoiGianDanhGia.Value;
            }
        }
    }
}