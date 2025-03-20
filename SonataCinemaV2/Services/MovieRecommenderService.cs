using Microsoft.ML;
using Microsoft.ML.Trainers;
using SonataCinemaV2.ML;
using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SonataCinemaV2.ML.MovieRecommenderModel;

namespace SonataCinemaV2.Services
{
    public class MovieRecommenderService
    {
        private readonly CinemaV3Entities db;

        public MovieRecommenderService(CinemaV3Entities context)
        {
            db = context;
        }

        public List<Phim> GetMovieRecommendations(int userId, int count = 3)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"\n=== Bắt đầu gợi ý phim cho user {userId} ===");

                var totalRatings = db.DanhGias.Count();
                System.Diagnostics.Debug.WriteLine($"Tổng số đánh giá trong hệ thống: {totalRatings}");

                if (totalRatings < 5)
                {
                    System.Diagnostics.Debug.WriteLine("Chưa đủ dữ liệu đánh giá, trả về phim ngẫu nhiên");
                    return GetRandomMovies(count);
                }

                var userRatings = db.DanhGias
                    .Where(d => d.ID_KhachHang == userId)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Số đánh giá của user: {userRatings.Count}");

                if (!userRatings.Any())
                {
                    System.Diagnostics.Debug.WriteLine("User chưa có đánh giá nào, trả về phim ngẫu nhiên");
                    return GetRandomMovies(count);
                }

                var highRatedMovies = userRatings
                    .Where(r => r.DiemDanhGia >= 4)
                    .Select(r => r.Phim)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Số phim được user đánh giá cao (>=4): {highRatedMovies.Count}");

                var favoriteGenres = new HashSet<string>();
                foreach (var movie in highRatedMovies)
                {
                    if (!string.IsNullOrEmpty(movie.TheLoai))
                    {
                        var genres = movie.TheLoai.Split(',').Select(g => g.Trim());
                        foreach (var genre in genres)
                        {
                            favoriteGenres.Add(genre);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Thể loại yêu thích: {string.Join(", ", favoriteGenres)}");

                var ratedMovieIds = userRatings.Select(r => r.ID_Phim).ToList();
                var recommendedMovies = db.Phims
                    .Where(p => !ratedMovieIds.Contains(p.ID_Phim))
                    .OrderByDescending(p => p.DanhGia)
                    .Take(count * 2)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Số phim tiềm năng để gợi ý: {recommendedMovies.Count}");

                var sortedMovies = recommendedMovies
                    .Select(movie => new
                    {
                        Movie = movie,
                        MatchingGenres = !string.IsNullOrEmpty(movie.TheLoai)
                            ? movie.TheLoai.Split(',')
                                .Select(g => g.Trim())
                                .Count(g => favoriteGenres.Contains(g))
                            : 0
                    })
                    .OrderByDescending(m => m.MatchingGenres)
                    .ThenByDescending(m => m.Movie.DanhGia)
                    .Select(m => m.Movie)
                    .Take(count)
                    .ToList();

                System.Diagnostics.Debug.WriteLine("\nPhim được gợi ý:");
                foreach (var movie in sortedMovies)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"- {movie.TenPhim} | Thể loại: {movie.TheLoai} | Đánh giá: {movie.DanhGia}");
                }

                if (sortedMovies.Count < count)
                {
                    var existingIds = sortedMovies.Select(m => m.ID_Phim).ToList();
                    var remainingCount = count - sortedMovies.Count;
                    var randomMovies = GetRandomMovies(remainingCount, existingIds);
                    sortedMovies.AddRange(randomMovies);

                    System.Diagnostics.Debug.WriteLine($"\nBổ sung {remainingCount} phim ngẫu nhiên:");
                    foreach (var movie in randomMovies)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"- {movie.TenPhim} | Thể loại: {movie.TheLoai} | Đánh giá: {movie.DanhGia}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== Kết thúc gợi ý phim ===\n");
                return sortedMovies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gợi ý phim: {ex.Message}");
                return GetRandomMovies(count);
            }
        }

        private List<Phim> GetRandomMovies(int count, List<int> excludeIds = null)
        {
            var query = db.Phims.AsQueryable();

            if (excludeIds != null && excludeIds.Any())
            {
                query = query.Where(p => !excludeIds.Contains(p.ID_Phim));
            }

            return query.OrderBy(r => Guid.NewGuid()).Take(count).ToList();
        }
    }
}