document.addEventListener('DOMContentLoaded', function () {
    var html = document.documentElement;
    if (html.getAttribute('data-theme') === 'system') {
        var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        html.setAttribute('data-theme', prefersDark ? 'dark' : 'light');
    }

    var toggleBtn = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('pvSidebar');
    var backdrop = document.getElementById('sidebarBackdrop');

    function setMobileMenu(open) {
        if (!sidebar) return;
        sidebar.classList.toggle('show', open);
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    if (toggleBtn && sidebar) toggleBtn.addEventListener('click', function () { setMobileMenu(!sidebar.classList.contains('show')); });
    if (backdrop) backdrop.addEventListener('click', function () { setMobileMenu(false); });
    if (sidebar) sidebar.querySelectorAll('nav a').forEach(function (link) { link.addEventListener('click', function () { if (window.matchMedia('(max-width: 991.98px)').matches) setMobileMenu(false); }); });
    document.addEventListener('keydown', function (event) { if (event.key === 'Escape') setMobileMenu(false); });

    var collapseBtn = document.getElementById('sidebarCollapseBtn');
    if (collapseBtn && sidebar) {
        collapseBtn.addEventListener('click', function () {
            var isCollapsed = sidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed ? 'true' : 'false');
            collapseBtn.setAttribute('title', isCollapsed ? 'Expand menu' : 'Collapse menu');
        });
        if (sidebar.classList.contains('collapsed')) collapseBtn.setAttribute('title', 'Expand menu');
    }

    document.querySelectorAll('.alert-auto-dismiss').forEach(function (alertEl) {
        setTimeout(function () { alertEl.classList.remove('show'); alertEl.classList.add('fade'); }, 4000);
    });
});

/* PUBLIC PORTAL - ABOUT HERO SLIDESHOW */
document.addEventListener('DOMContentLoaded', function () {
    const slider = document.querySelector('.about-hero-slider');
    if (!slider) return;
    const slides = slider.querySelectorAll('.about-slide');
    const dots = slider.querySelectorAll('.about-slider-dot');
    const previousButton = slider.querySelector('.about-slider-prev');
    const nextButton = slider.querySelector('.about-slider-next');
    if (slides.length === 0) return;
    let currentSlide = 0;
    let slideTimer;
    const slideInterval = 5000;
    function showSlide(index) {
        if (index >= slides.length) index = 0;
        if (index < 0) index = slides.length - 1;
        slides.forEach(function (slide) { slide.classList.remove('active'); });
        dots.forEach(function (dot) { dot.classList.remove('active'); });
        slides[index].classList.add('active');
        if (dots[index]) dots[index].classList.add('active');
        currentSlide = index;
    }
    function nextSlide() { showSlide(currentSlide + 1); }
    function previousSlide() { showSlide(currentSlide - 1); }
    function startSlideshow() { clearInterval(slideTimer); slideTimer = setInterval(nextSlide, slideInterval); }
    function restartSlideshow() { clearInterval(slideTimer); startSlideshow(); }
    if (nextButton) nextButton.addEventListener('click', function () { nextSlide(); restartSlideshow(); });
    if (previousButton) previousButton.addEventListener('click', function () { previousSlide(); restartSlideshow(); });
    dots.forEach(function (dot, index) { dot.addEventListener('click', function () { showSlide(index); restartSlideshow(); }); });
    showSlide(0);
    startSlideshow();
});

/* PUBLIC PORTAL - PASSWORD VISIBILITY */
document.addEventListener('DOMContentLoaded', function () {
    const passwordInput = document.getElementById('loginPassword') || document.getElementById('password');
    const togglePassword = document.getElementById('togglePasswordBtn') || document.getElementById('togglePassword');
    if (!passwordInput || !togglePassword) return;
    togglePassword.addEventListener('click', function () {
        const icon = togglePassword.querySelector('i');
        const show = passwordInput.type === 'password';
        passwordInput.type = show ? 'text' : 'password';
        if (icon) {
            icon.classList.toggle('bi-eye', !show);
            icon.classList.toggle('bi-eye-slash', show);
        }
        togglePassword.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
    });
});

/* PUBLIC PORTAL - BACK TO TOP BUTTON */
document.addEventListener('DOMContentLoaded', function () {
    const backToTopButton = document.getElementById('backToTopBtn');
    if (!backToTopButton) return;
    function toggleBackToTopButton() { backToTopButton.classList.toggle('show', window.scrollY > 350); }
    window.addEventListener('scroll', toggleBackToTopButton, { passive: true });
    backToTopButton.addEventListener('click', function () { window.scrollTo({ top: 0, behavior: 'smooth' }); });
    toggleBackToTopButton();
});
