// Khởi tạo khi document ready
$(document).ready(function () {
    initializeComboHandlers();
    // Thêm handler cho modal khi đóng
    $('#comboModal').on('hidden.bs.modal', function () {
        resetComboForm();
    });
});

// Khởi tạo các handlers
function initializeComboHandlers() {
    // Preview hình ảnh
    $('#HinhAnh').on('change', function (e) {
        previewImage(this);
    });

    // Filter và search
    $('#searchCombo, #statusFilter').on('change keyup', function () {
        filterCombos();
    });
}

// Hiển thị modal thêm/sửa combo
function showComboModal() {
    resetComboForm();
    $('#modalTitle').text('Thêm Combo Mới');
    $('#comboModal').modal('show');
}

// Reset form
function resetComboForm() {
    $('#comboForm')[0].reset();
    $('#ID_Combo').val('');
    $('#imagePreview').empty();
    clearValidationErrors();
}

// Xóa validation errors
function clearValidationErrors() {
    $('.is-invalid').removeClass('is-invalid');
    $('.invalid-feedback').remove();
}

// Preview hình ảnh
function previewImage(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            $('#imagePreview').html(`
                <img src="${e.target.result}" class="img-thumbnail" style="max-height: 200px;">
            `);
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Lưu combo
function saveCombo() {
    if (!validateComboForm()) return;

    const formData = new FormData($('#comboForm')[0]);
    const comboId = $('#ID_Combo').val();
    const url = comboId ? '/Combo/Edit' : '/Combo/Create';

    if (!comboId) {
        formData.delete('ID_Combo');
    }
    // Thêm token vào formData
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    title: 'Thành công!',
                    text: response.message,
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    $('#comboModal').modal('hide');
                    reloadComboList();
                });
            } else {
                Swal.fire('Lỗi!', response.message, 'error');
            }
        },
        error: function (xhr, status, error) {
            Swal.fire('Lỗi!', 'Có lỗi xảy ra khi lưu combo: ' + error, 'error');
        }
    });
}

// Validate form
function validateComboForm() {
    clearValidationErrors();
    let isValid = true;

    if (!$('#TenCombo').val().trim()) {
        showInputError('#TenCombo', 'Vui lòng nhập tên combo');
        isValid = false;
    }

    if (!$('#MoTa').val().trim()) {
        showInputError('#MoTa', 'Vui lòng nhập mô tả');
        isValid = false;
    }

    if (!$('#Gia').val() || $('#Gia').val() <= 0) {
        showInputError('#Gia', 'Vui lòng nhập giá hợp lệ');
        isValid = false;
    }

    return isValid;
}

// Hiển thị lỗi input
function showInputError(inputId, message) {
    const input = $(inputId);
    input.addClass('is-invalid');
    input.after(`<div class="invalid-feedback">${message}</div>`);
}

// Sửa combo
function editCombo(id) {
    $.get('/Combo/GetComboById', { id: id })
        .done(function (response) {
            if (response.success) {
                resetComboForm();
                const combo = response.data;
                $('#modalTitle').text('Chỉnh Sửa Combo');
                $('#ID_Combo').val(combo.ID_Combo);
                $('#TenCombo').val(combo.TenCombo);
                $('#MoTa').val(combo.MoTa);
                $('#Gia').val(combo.Gia);

                if (combo.HinhAnh) {
                    $('#imagePreview').html(`
                        <img src="${combo.HinhAnh}" class="img-thumbnail" style="max-height: 200px;">
                    `);
                }

                $('#comboModal').modal('show');
            } else {
                Swal.fire('Lỗi!', response.message, 'error');
            }
        })
        .fail(function () {
            Swal.fire('Lỗi!', 'Không thể tải thông tin combo', 'error');
        });
}

// Xóa combo
function deleteCombo(id) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    Swal.fire({
        title: 'Xác nhận xóa?',
        text: 'Bạn có chắc muốn xóa combo này?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Xóa',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({  // Thay $.post bằng $.ajax để thêm header
                url: '/Combo/Delete',
                type: 'POST',
                data: { id: id },
                headers: {
                    'RequestVerificationToken': token
                },
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            title: 'Thành công!',
                            text: response.message,
                            icon: 'success',
                            timer: 2000,
                            showConfirmButton: false
                        }).then(() => {
                            reloadComboList();
                        });
                    } else {
                        Swal.fire('Lỗi!', response.message, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Lỗi!', 'Có lỗi xảy ra khi xóa combo', 'error');
                }
            });
        }
    });
}

// Toggle trạng thái
function toggleComboStatus(id) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Combo/ToggleStatus',
        type: 'POST',
        data: { id: id },
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                reloadComboList();
            } else {
                Swal.fire('Lỗi!', response.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Lỗi!', 'Có lỗi xảy ra khi thay đổi trạng thái', 'error');
        }
    });
}

// Filter combos
function filterCombos() {
    const search = $('#searchCombo').val().toLowerCase();
    const status = $('#statusFilter').val();

    $('.col-xl-3').each(function () {  // Thay vì .combo-card
        const card = $(this);
        const title = card.find('.card-title').text().toLowerCase();
        const isActive = card.find('.badge').hasClass('bg-success');
        const cardStatus = isActive.toString();

        let show = true;
        if (search && !title.includes(search)) show = false;
        if (status && status !== '' && cardStatus !== status) show = false;

        card.toggle(show);
    });
}

// Reload danh sách combo
function reloadComboList() {
    $.get('/Combo/QuanLyComboPartial', function (data) {
        $('.combo-management').html(data);
        initializeComboHandlers();
    });
}