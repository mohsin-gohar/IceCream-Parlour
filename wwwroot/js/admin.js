/* iCREAM Admin JavaScript */
(function ($) {
    'use strict';

    $(document).ready(function () {
        // Page loader
        setTimeout(function () {
            $('#pageLoader').addClass('hidden');
        }, 400);

        // Sidebar toggle
        $('#sidebarToggle').on('click', function () {
            $('#sidebar').toggleClass('open');
            if (!$('#sidebarOverlay').length) {
                $('body').append('<div class="sidebar-overlay" id="sidebarOverlay"></div>');
            }
            $('#sidebarOverlay').toggleClass('active');
        });

        $(document).on('click', '#sidebarOverlay', function () {
            $('#sidebar').removeClass('open');
            $(this).removeClass('active');
        });

        // Toast notifications
        $('.toast-alert').each(function () {
            var $toast = $(this);
            setTimeout(function () {
                $toast.fadeOut(400, function () { $(this).remove(); });
            }, 4000);
        });

        // SweetAlert2 delete confirmations
        $(document).on('click', '[data-confirm-delete]', function (e) {
            e.preventDefault();
            var form = $(this).closest('form');
            var message = $(this).data('confirm-delete') || 'This action cannot be undone.';
            if (typeof Swal === 'undefined') {
                if (window.confirm(message)) { form.trigger('submit'); }
                return;
            }

            Swal.fire({
                title: 'Are you sure?',
                text: message,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#e84393',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Yes, delete it!'
            }).then(function (result) {
                if (result.isConfirmed) { form.trigger('submit'); }
            });
        });

        // DataTables initialization
        if ($.fn.DataTable && $('.datatable').length) {
            $('.datatable').DataTable({
                pageLength: 10,
                responsive: true,
                language: {
                    search: '',
                    searchPlaceholder: 'Search records...',
                    lengthMenu: 'Show _MENU_ entries',
                    info: 'Showing _START_ to _END_ of _TOTAL_ entries'
                },
                dom: '<"row mb-3"<"col-sm-6"l><"col-sm-6"f>>rtip'
            });
        }

        // AOS animations
        if (typeof AOS !== 'undefined') {
            AOS.init({ duration: 600, once: true, offset: 50 });
        }

        // Image preview on file input
        $(document).on('change', '[data-image-preview]', function () {
            var target = $($(this).data('image-preview'));
            target.empty();
            var files = this.files;
            for (var i = 0; i < files.length; i++) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    target.append(
                        '<div class="image-preview-item"><img src="' + e.target.result + '" alt="Preview"></div>'
                    );
                };
                reader.readAsDataURL(files[i]);
            }
        });

        // Order line add/remove
        $('#addOrderLine').on('click', function () {
            var template = $('#orderLineTemplate').html();
            if (!template) { return; }

            // Each row needs its own index so model binding receives all lines.
            var nextIndex = $('#orderLines .order-line').length;
            $('#orderLines').append(template.split('__index__').join(nextIndex));
        });

        $(document).on('click', '.remove-order-line', function () {
            if ($('#orderLines .order-line').length > 1) {
                $(this).closest('.order-line').remove();
            }
        });

        // Print invoice
        $('#printInvoice').on('click', function () {
            window.print();
        });

        // Report type tabs
        $('.report-tab').on('click', function (e) {
            e.preventDefault();
            var type = $(this).data('type');
            $('#reportType').val(type);
            $('.report-tab').removeClass('active');
            $(this).addClass('active');
            $('#reportForm').submit();
        });
    });

    // Chart helper
    window.createRevenueChart = function (canvasId, labels, data) {
        var ctx = document.getElementById(canvasId);
        if (!ctx || typeof Chart === 'undefined') return;

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Revenue ($)',
                    data: data,
                    borderColor: '#e84393',
                    backgroundColor: 'rgba(232, 67, 147, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#e84393',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        ticks: {
                            callback: function (v) { return '$' + v; }
                        }
                    },
                    x: { grid: { display: false } }
                }
            }
        });
    };

    window.createBarChart = function (canvasId, labels, data) {
        var ctx = document.getElementById(canvasId);
        if (!ctx || typeof Chart === 'undefined') return;

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Sales ($)',
                    data: data,
                    backgroundColor: 'rgba(0, 180, 216, 0.7)',
                    borderColor: '#00b4d8',
                    borderWidth: 1,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { callback: function (v) { return '$' + v; } }
                    }
                }
            }
        });
    };
})(jQuery);




