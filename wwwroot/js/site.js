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

/* ATTENDANCE DASHBOARD */
document.addEventListener('DOMContentLoaded', function () {
    var path = window.location.pathname.replace(/\/+$/, '').toLowerCase();
    if (path !== '/attendance' && path !== '/attendance/index') return;

    var recordsCard = Array.from(document.querySelectorAll('.pv-card')).find(function (card) {
        var heading = card.querySelector('h5');
        return heading && heading.textContent.trim() === 'Attendance Records';
    });

    if (!recordsCard) return;

    var iconBox = recordsCard.querySelector('.icon-box');
    if (iconBox) {
        iconBox.style.width = '48px';
        iconBox.style.height = '48px';
        iconBox.style.borderRadius = '.75rem';
        iconBox.style.display = 'flex';
        iconBox.style.alignItems = 'center';
        iconBox.style.justifyContent = 'center';
        iconBox.style.flexShrink = '0';
        iconBox.style.backgroundColor = 'var(--pv-green)';
        iconBox.style.color = '#fff';
        iconBox.style.fontSize = '1.3rem';

        var icon = iconBox.querySelector('i');
        if (icon) {
            icon.className = 'bi bi-clipboard2-check-fill';
            icon.style.color = '#fff';
            icon.style.fontSize = '1.3rem';
            icon.style.lineHeight = '1';
        }
    }

    var actions = recordsCard.querySelector('.d-flex.flex-wrap.gap-2');

    if (actions && !document.getElementById('liveAttendancePageButton')) {
        var liveButton = document.createElement('a');
        liveButton.id = 'liveAttendancePageButton';
        liveButton.href = '/Attendance/Live';
        liveButton.className = 'btn btn-pv';
        liveButton.innerHTML = '<i class="bi bi-qr-code-scan me-1"></i> Live Attendance';
        actions.appendChild(liveButton);
    }
});

/* LIVE ATTENDANCE - GROUP EVENT PICKER BY YEAR AND MONTH */
document.addEventListener('DOMContentLoaded', function () {
    var path = window.location.pathname.replace(/\/+$/, '').toLowerCase();
    if (path !== '/attendance/live') return;

    var eventPicker = document.getElementById('eventPicker');
    if (!eventPicker || eventPicker.querySelector('optgroup')) return;

    var options = Array.from(eventPicker.querySelectorAll('option'));
    if (!options.length) return;

    var selectedValue = eventPicker.value;
    var monthNames = {
        jan: 'January',
        feb: 'February',
        mar: 'March',
        apr: 'April',
        may: 'May',
        jun: 'June',
        jul: 'July',
        aug: 'August',
        sep: 'September',
        oct: 'October',
        nov: 'November',
        dec: 'December'
    };

    var groups = new Map();
    var fallbackOptions = [];

    options.forEach(function (option) {
        if (!option.value) {
            fallbackOptions.push(option);
            return;
        }

        var text = option.textContent.trim();
        var match = text.match(/^(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})\s+—\s+(.+)$/);

        if (!match) {
            fallbackOptions.push(option);
            return;
        }

        var monthKey = match[2].slice(0, 3).toLowerCase();
        var monthName = monthNames[monthKey];

        if (!monthName) {
            fallbackOptions.push(option);
            return;
        }

        var year = match[3];
        var groupKey = year + '-' + monthKey;

        if (!groups.has(groupKey)) {
            var group = document.createElement('optgroup');
            group.label = year + ' — ' + monthName;
            groups.set(groupKey, group);
        }

        groups.get(groupKey).appendChild(option);
    });

    if (!groups.size) return;

    eventPicker.innerHTML = '';

    fallbackOptions.forEach(function (option) {
        eventPicker.appendChild(option);
    });

    groups.forEach(function (group) {
        eventPicker.appendChild(group);
    });

    eventPicker.value = selectedValue;
    eventPicker.setAttribute('aria-label', 'Events grouped by year and month');
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
