function playTrailer(trailerUrl) {
    const modal = document.getElementById('trailerModal');
    const iframe = document.getElementById('trailerFrame');

    // Chuyển đổi URL YouTube thành embed URL và lấy videoId
    let videoId = '';

    if (trailerUrl.includes('watch?v=')) {
        videoId = trailerUrl.split('watch?v=')[1];
    } else if (trailerUrl.includes('youtu.be/')) {
        videoId = trailerUrl.split('youtu.be/')[1];
    } else if (trailerUrl.includes('embed/')) {
        videoId = trailerUrl.split('embed/')[1];
    }

    // Loại bỏ các parameter phụ nếu có
    if (videoId.includes('&')) {
        videoId = videoId.split('&')[0];
    }

    // Tạo embed URL với các parameter cần thiết
    const embedUrl = `https://www.youtube.com/embed/${videoId}?autoplay=1&rel=0&showinfo=0`;

    // Set src cho iframe
    iframe.src = embedUrl;

    // Hiển thị modal
    modal.style.display = 'block';

    // Log để debug
    console.log('Original URL:', trailerUrl);
    console.log('Video ID:', videoId);
    console.log('Embed URL:', embedUrl);
}

// Đóng modal
document.querySelector('.close-modal').onclick = function () {
    const modal = document.getElementById('trailerModal');
    const iframe = document.getElementById('trailerFrame');
    modal.style.display = 'none';
    iframe.src = ''; // Dừng video khi đóng modal
}

// Đóng modal khi click ngoài
window.onclick = function (event) {
    const modal = document.getElementById('trailerModal');
    if (event.target == modal) {
        modal.style.display = 'none';
        document.getElementById('trailerFrame').src = '';
    }
}