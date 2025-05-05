
// Khởi tạo khi document ready
document.addEventListener("DOMContentLoaded", function () {
    initializeNavigation();
    initializeFormHandlers();
    initializeModalHandlers();
    registerEventHandlers();
});

// Xử lý navigation
function initializeNavigation() {
    const links = document.querySelectorAll('.nav-link');
    const manageButtons = document.querySelectorAll(".nav-link");
    const contentElements = {
        home: document.querySelector(".home-content"),
        employee: document.querySelector(".employee-management"),
        movie: document.querySelector(".movie-management"),
        screeningRoom: document.querySelector(".screening-room-management"),
        customer: document.querySelector(".customer-management"),
        showtime: document.querySelector(".showtime-management"),
        blog: document.querySelector(".blog-management"),
        ticket: document.querySelector(".ticket-management"),
        combo: document.querySelector(".combo-management"),
        helper: document.querySelector(".helper-management"),
        report: document.querySelector(".report-management")
    };

    // Active state cho navigation
    links.forEach(link => {
        link.addEventListener('click', function () {
            links.forEach(l => l.classList.remove('active'));
            this.classList.add('active');
        });
    });

    // Xử lý chuyển đổi tab content
    manageButtons.forEach(button => {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            hideAllContent();
            showSelectedContent(this.textContent.trim());
        });
    });

    function hideAllContent() {
        Object.values(contentElements).forEach(content => {
            if (content) content.classList.add("d-none");
        });
    }

    function showSelectedContent(buttonText) {
        const contentMap = {
            "Trang chủ": contentElements.home,
            "Quản lý Nhân viên": contentElements.employee,
            "Quản lý Phim": contentElements.movie,
            "Quản lý Phòng Chiếu": contentElements.screeningRoom,
            "Quản lý Khách Hàng": contentElements.customer,
            "Quản lý Lịch Chiếu": contentElements.showtime,
            "Quản lý Blog": contentElements.blog,
            "Quản lý Vé": contentElements.ticket,
            "Quản lý Dịch vụ": contentElements.combo,
            "Hỗ trợ Khách hàng": contentElements.helper,
            "Báo Cao Doanh Thu": contentElements.report
        };

        const content = contentMap[buttonText];
        if (content) content.classList.remove("d-none");
    }
}
// Add to your existing JavaScript file
function toggleSidebar() {
    const sidebar = document.querySelector('.admin-sidebar');
    const mainWrapper = document.querySelector('.main-wrapper');
    const overlay = document.querySelector('.sidebar-overlay');
    
    sidebar.classList.toggle('show');
    
    if (!overlay) {
        const newOverlay = document.createElement('div');
        newOverlay.className = 'sidebar-overlay';
        document.body.appendChild(newOverlay);
        
        newOverlay.addEventListener('click', () => {
            sidebar.classList.remove('show');
            newOverlay.remove();
        });
    } else {
        overlay.remove();
    }
}

// Close sidebar when clicking outside on mobile
document.addEventListener('click', (e) => {
    const sidebar = document.querySelector('.admin-sidebar');
    const toggleBtn = document.querySelector('.toggle-sidebar');
    
    if (window.innerWidth <= 768 
        && sidebar.classList.contains('show') 
        && !sidebar.contains(e.target) 
        && e.target !== toggleBtn) {
        sidebar.classList.remove('show');
        const overlay = document.querySelector('.sidebar-overlay');
        if (overlay) overlay.remove();
    }
});

// Handle window resize
window.addEventListener('resize', () => {
    const sidebar = document.querySelector('.admin-sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    
    if (window.innerWidth > 768) {
        sidebar.classList.remove('show');
        if (overlay) overlay.remove();
    }
});