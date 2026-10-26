/* iCREAM - public site scripts.
   Third-party plugins (Owl Carousel, Isotope) are optional; every call is guarded
   so a missing library can never break the rest of the page. */
(function ($) {
    'use strict';

    // ===== Smooth scrolling for in-page links =====
    $('.navbar-nav a').on('click', function (event) {
        var hash = this.hash;
        if (hash && hash.length > 1 && $(hash).length) {
            event.preventDefault();
            $('html, body').animate({
                scrollTop: $(hash).offset().top - 45
            }, 800, 'swing');
        }
    });

    // ===== Back to top =====
    $(window).on('scroll', function () {
        if ($(this).scrollTop() > 100) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });

    $(document).on('click', '.back-to-top', function () {
        $('html, body').animate({ scrollTop: 0 }, 600, 'swing');
        return false;
    });

    // ===== Video modal (Bootstrap 5) =====
    var $videoSrc = '';
    $(document).on('click', '.btn-play', function () {
        $videoSrc = $(this).data('src') || '';
    });

    $('#videoModal').on('shown.bs.modal', function () {
        if ($videoSrc) {
            $('#video')
                .attr('src', $videoSrc + '?autoplay=1&modestbranding=1&showinfo=0')
                .show();
        }
    });

    $('#videoModal').on('hide.bs.modal', function () {
        $('#video').attr('src', $videoSrc);
    });

    // ===== Optional carousels =====
    if ($.fn.owlCarousel) {
        $('.service-carousel, .team-carousel').owlCarousel({
            autoplay: false,
            smartSpeed: 1500,
            margin: 30,
            dots: false,
            loop: true,
            nav: true,
            navText: [
                '<i class="fa fa-angle-left" aria-hidden="true"></i>',
                '<i class="fa fa-angle-right" aria-hidden="true"></i>'
            ],
            responsive: { 0: { items: 1 }, 576: { items: 1 }, 768: { items: 2 }, 992: { items: 3 } }
        });

        $('.product-carousel').owlCarousel({
            autoplay: false,
            smartSpeed: 1500,
            margin: 30,
            dots: false,
            loop: true,
            nav: true,
            navText: [
                '<i class="fa fa-angle-left" aria-hidden="true"></i>',
                '<i class="fa fa-angle-right" aria-hidden="true"></i>'
            ],
            responsive: { 0: { items: 1 }, 576: { items: 2 }, 768: { items: 3 }, 992: { items: 4 } }
        });

        $('.testimonial-carousel').owlCarousel({
            autoplay: true,
            smartSpeed: 1500,
            dots: true,
            loop: true,
            items: 1
        });
    }

    // ===== Optional portfolio filter =====
    if ($.fn.isotope && $('.portfolio-container').length) {
        var $portfolio = $('.portfolio-container').isotope({
            itemSelector: '.portfolio-item',
            layoutMode: 'fitRows'
        });

        $('#portfolio-flters li').on('click', function () {
            $('#portfolio-flters li').removeClass('active');
            $(this).addClass('active');
            $portfolio.isotope({ filter: $(this).data('filter') });
        });
    }

})(jQuery);

// ===== Vanilla helpers =====
document.addEventListener('DOMContentLoaded', function () {

    function prefersReducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    // ---- Animated counters ----
    var counters = document.querySelectorAll('.counter-number');
    if (counters.length) {
        var animated = false;

        var startCounters = function () {
            if (animated || prefersReducedMotion()) {
                counters.forEach(function (counter) {
                    counter.innerText = parseInt(counter.getAttribute('data-target') || '0', 10).toLocaleString();
                });
                return;
            }

            animated = true;
            counters.forEach(function (counter) {
                var target = parseInt(counter.getAttribute('data-target') || '0', 10);
                if (!target || target < 0) { return; }

                var current = 0;
                var step = Math.max(1, Math.ceil(target / 60));

                var tick = function () {
                    current += step;
                    if (current < target) {
                        counter.innerText = current.toLocaleString();
                        window.setTimeout(tick, 25);
                    } else {
                        counter.innerText = target.toLocaleString();
                    }
                };

                tick();
            });
        };

        var counterSection = document.querySelector('.counter-section');
        if (counterSection && 'IntersectionObserver' in window) {
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        startCounters();
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.3 });

            observer.observe(counterSection);
        } else {
            startCounters();
        }
    }

    // ---- Testimonial slider ----
    var slides = document.querySelectorAll('.testimonial-slide');
    var dots = document.querySelectorAll('.dot');
    if (slides.length) {
        var current = 0;
        var autoTimer = null;

        var goTo = function (index) {
            slides[current].classList.remove('active');
            if (dots[current]) { dots[current].classList.remove('active'); }

            var previous = current;
            current = (index + slides.length) % slides.length;
            slides[current].classList.add('active');
            if (dots[current]) { dots[current].classList.add('active'); }

            if (slides[previous]) { slides[previous].classList.remove('active'); }
        };

        var next = function () { goTo(current + 1); };
        var prev = function () { goTo(current - 1); };
        var startAuto = function () {
            if (prefersReducedMotion()) { return; }
            autoTimer = window.setInterval(next, 5000);
        };
        var resetAuto = function () {
            window.clearInterval(autoTimer);
            startAuto();
        };

        var nextBtn = document.getElementById('nextBtn');
        var prevBtn = document.getElementById('prevBtn');
        if (nextBtn) { nextBtn.addEventListener('click', function () { next(); resetAuto(); }); }
        if (prevBtn) { prevBtn.addEventListener('click', function () { prev(); resetAuto(); }); }

        dots.forEach(function (dot, index) {
            dot.addEventListener('click', function () {
                goTo(parseInt(dot.dataset.index, 10) || index);
                resetAuto();
            });
        });

        startAuto();
    }

    // ---- Mobile menu toggle ----
    var toggler = document.getElementById('navbarToggler');
    var collapse = document.getElementById('navbarCollapse');
    if (toggler && collapse) {
        toggler.addEventListener('click', function () {
            collapse.classList.toggle('show');
        });
    }

    // ---- Auto-dismiss flash messages ----
    document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
        window.setTimeout(function () {
            if (window.bootstrap && window.bootstrap.Alert) {
                window.bootstrap.Alert.getOrCreateInstance(alert).close();
            } else {
                alert.remove();
            }
        }, 7000);
    });

    // ---- Prevent accidental double submits ----
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            var submit = form.querySelector('button[type="submit"]');
            if (!submit || submit.disabled) { return; }
            window.setTimeout(function () { submit.disabled = true; }, 10);
        });
    });
});
