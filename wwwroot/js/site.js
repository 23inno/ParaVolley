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

/* ATTENDANCE - RECORD ATTENDANCE + LIVE CHECK-INS */
document.addEventListener('DOMContentLoaded', function () {
    var path = window.location.pathname.replace(/\/+$/, '').toLowerCase();
    if (!path.startsWith('/attendance')) return;

    if (path === '/attendance' || path === '/attendance/index') {
        var recordsCard = Array.from(document.querySelectorAll('.pv-card')).find(function (card) {
            var heading = card.querySelector('h5');
            return heading && heading.textContent.trim() === 'Attendance Records';
        });

        if (recordsCard) {
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
                    icon.className = 'bi bi-clipboard-check-fill';
                    icon.style.color = '#fff';
                    icon.style.fontSize = '1.3rem';
                    icon.style.lineHeight = '1';
                }
            }

            var liveCard = document.getElementById('liveAttendanceCard');
            if (!liveCard) {
                liveCard = document.createElement('div');
                liveCard.id = 'liveAttendanceCard';
                liveCard.className = 'pv-card mb-3';

                var header = document.createElement('div');
                header.className = 'd-flex flex-wrap justify-content-between align-items-center gap-2 mb-3';

                var titleWrap = document.createElement('div');
                var title = document.createElement('h5');
                title.className = 'mb-1';
                title.textContent = 'Live Attendance Check-ins';
                var subtitle = document.createElement('p');
                subtitle.className = 'text-muted small mb-0';
                subtitle.textContent = 'Player QR submissions and other attendance changes appear here automatically.';
                titleWrap.appendChild(title);
                titleWrap.appendChild(subtitle);

                var controls = document.createElement('div');
                controls.className = 'd-flex align-items-center gap-2';
                var liveBadge = document.createElement('span');
                liveBadge.className = 'badge bg-success-subtle text-success border border-success-subtle';
                liveBadge.innerHTML = '<i class="bi bi-broadcast-pin me-1"></i> Live';
                var viewAll = document.createElement('a');
                viewAll.href = '/Attendance/Records';
                viewAll.className = 'btn btn-sm btn-outline-secondary';
                viewAll.innerHTML = '<i class="bi bi-list-check me-1"></i> View all records';
                controls.appendChild(liveBadge);
                controls.appendChild(viewAll);

                header.appendChild(titleWrap);
                header.appendChild(controls);
                liveCard.appendChild(header);

                var tableWrap = document.createElement('div');
                tableWrap.className = 'table-responsive';
                var table = document.createElement('table');
                table.className = 'table align-middle mb-0';
                table.innerHTML = '<thead><tr><th>Player</th><th>Event</th><th>Date</th><th>Status</th></tr></thead><tbody id="liveAttendanceBody"><tr><td colspan="4" class="text-center text-muted py-3">Loading attendance...</td></tr></tbody>';
                tableWrap.appendChild(table);
                liveCard.appendChild(tableWrap);

                recordsCard.insertAdjacentElement('afterend', liveCard);
            }

            var liveBody = document.getElementById('liveAttendanceBody');
            var liveRequestInFlight = false;

            function renderLiveAttendance(records) {
                if (!liveBody) return;
                liveBody.innerHTML = '';

                if (!records || records.length === 0) {
                    var emptyRow = document.createElement('tr');
                    var emptyCell = document.createElement('td');
                    emptyCell.colSpan = 4;
                    emptyCell.className = 'text-center text-muted py-3';
                    emptyCell.textContent = 'No attendance records yet.';
                    emptyRow.appendChild(emptyCell);
                    liveBody.appendChild(emptyRow);
                    return;
                }

                records.forEach(function (record) {
                    var row = document.createElement('tr');

                    var playerCell = document.createElement('td');
                    playerCell.className = 'fw-semibold';
                    playerCell.textContent = record.playerName;
                    row.appendChild(playerCell);

                    var eventCell = document.createElement('td');
                    eventCell.textContent = record.eventTitle;
                    row.appendChild(eventCell);

                    var dateCell = document.createElement('td');
                    dateCell.className = 'text-muted';
                    dateCell.textContent = record.dateLabel;
                    row.appendChild(dateCell);

                    var statusCell = document.createElement('td');
                    var badge = document.createElement('span');
                    badge.className = 'badge ' + (record.status === 'Present' ? 'badge-active' : 'badge-cancelled');
                    badge.textContent = record.status;
                    statusCell.appendChild(badge);
                    row.appendChild(statusCell);

                    liveBody.appendChild(row);
                });
            }

            function refreshLiveAttendance() {
                if (liveRequestInFlight) return;
                liveRequestInFlight = true;

                fetch('/Attendance/Live/Recent', {
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(function (response) {
                        if (!response.ok) throw new Error('Could not load live attendance.');
                        return response.json();
                    })
                    .then(renderLiveAttendance)
                    .catch(function () {
                        // Keep the previous successful feed on a temporary failure.
                    })
                    .finally(function () {
                        liveRequestInFlight = false;
                    });
            }

            refreshLiveAttendance();
            window.setInterval(refreshLiveAttendance, 5000);
        }
    }

    var modalLoadPromise = null;

    function ensureAttendanceModal() {
        var existing = document.getElementById('recordAttendanceModal');
        if (existing) return Promise.resolve(existing);
        if (modalLoadPromise) return modalLoadPromise;

        modalLoadPromise = fetch('/Attendance/Inline/Form', {
            method: 'GET',
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (response) {
                if (!response.ok) throw new Error('Could not load attendance form.');
                return response.text();
            })
            .then(function (html) {
                document.body.insertAdjacentHTML('beforeend', html);
                return document.getElementById('recordAttendanceModal');
            })
            .finally(function () {
                modalLoadPromise = null;
            });

        return modalLoadPromise;
    }

    function openAttendanceModal(fallbackUrl) {
        ensureAttendanceModal()
            .then(function (modalElement) {
                if (!modalElement || typeof bootstrap === 'undefined' || !bootstrap.Modal) {
                    window.location.href = fallbackUrl;
                    return;
                }

                var modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
                    backdrop: true,
                    keyboard: true,
                    focus: true
                });

                modalElement.addEventListener('shown.bs.modal', function focusFirstAttendanceField() {
                    modalElement.removeEventListener('shown.bs.modal', focusFirstAttendanceField);
                    var firstField = modalElement.querySelector('select, input:not([type="hidden"])');
                    if (firstField) firstField.focus();
                });

                modal.show();
            })
            .catch(function () {
                window.location.href = fallbackUrl;
            });
    }

    document.querySelectorAll('a').forEach(function (link) {
        var rawHref = link.getAttribute('href');
        if (!rawHref) return;

        var linkUrl;
        try {
            linkUrl = new URL(rawHref, window.location.origin);
        } catch (_) {
            return;
        }

        if (linkUrl.pathname.replace(/\/+$/, '').toLowerCase() !== '/attendance/create') return;

        link.addEventListener('click', function (event) {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            openAttendanceModal(link.href);
        });
    });

    if (window.location.hash.toLowerCase() === '#record-attendance-modal') {
        openAttendanceModal('/Attendance/Create');
    }
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
