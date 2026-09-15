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

    var recordButton = actions
        ? Array.from(actions.querySelectorAll('a, button')).find(function (item) {
            return item.textContent.trim().toLowerCase() === 'record attendance';
        })
        : null;

    if (!recordButton) return;

    recordButton.setAttribute('role', 'button');
    recordButton.setAttribute('aria-controls', 'attendanceRecordCollapse');
    recordButton.setAttribute('aria-expanded', 'false');

    var formHost = document.getElementById('attendanceRecordCollapse');
    if (!formHost) {
        formHost = document.createElement('div');
        formHost.id = 'attendanceRecordCollapse';
        formHost.className = 'collapse';
        recordsCard.insertAdjacentElement('afterend', formHost);
    }

    var formLoaded = false;
    var formLoading = false;

    function setRecordButtonState(expanded) {
        recordButton.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        recordButton.innerHTML = expanded
            ? '<i class="bi bi-chevron-up me-1"></i> Collapse Form'
            : '<i class="bi bi-plus-lg me-1"></i> Record Attendance';
    }

    async function ensureFormLoaded() {
        if (formLoaded) return true;
        if (formLoading) return false;

        formLoading = true;
        formHost.innerHTML = '<div class="pv-card mt-3 mb-3 text-center text-muted py-4"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Loading attendance form...</div>';

        try {
            var response = await fetch('/Attendance/Inline/Form', {
                method: 'GET',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Attendance form request failed with HTTP ' + response.status + '.');
            }

            formHost.innerHTML = await response.text();
            formLoaded = true;
            return true;
        } catch (error) {
            console.error(error);
            formHost.innerHTML = '<div class="alert alert-danger mt-3 mb-3">The attendance form could not be loaded. Please refresh the page and try again.</div>';
            return false;
        } finally {
            formLoading = false;
        }
    }

    async function toggleAttendanceForm(forceOpen) {
        var loaded = await ensureFormLoaded();
        if (!loaded && !formHost.innerHTML) return;

        var collapse = bootstrap.Collapse.getOrCreateInstance(formHost, { toggle: false });

        if (forceOpen === true) {
            collapse.show();
            return;
        }

        if (formHost.classList.contains('show')) {
            collapse.hide();
        } else {
            collapse.show();
        }
    }

    recordButton.addEventListener('click', function (event) {
        event.preventDefault();
        toggleAttendanceForm(false);
    });

    formHost.addEventListener('shown.bs.collapse', function () {
        setRecordButtonState(true);
    });

    formHost.addEventListener('hidden.bs.collapse', function () {
        setRecordButtonState(false);
    });

    formHost.addEventListener('click', function (event) {
        var closeButton = event.target.closest('[data-attendance-collapse-close]');
        if (!closeButton) return;

        event.preventDefault();
        bootstrap.Collapse.getOrCreateInstance(formHost, { toggle: false }).hide();
    });

    if (window.location.hash === '#record-attendance-card') {
        toggleAttendanceForm(true).then(function () {
            window.setTimeout(function () {
                var card = document.getElementById('record-attendance-card');
                if (card) card.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 150);
        });
    }
});

/* LIVE ATTENDANCE - EXPANDABLE EVENT TREE + EVENT TYPE FILTER */
document.addEventListener('DOMContentLoaded', function () {
    var path = window.location.pathname.replace(/\/+$/, '').toLowerCase();
    if (path !== '/attendance/live') return;

    var eventPicker = document.getElementById('eventPicker');
    if (!eventPicker) return;

    var selectedOption = eventPicker.options[eventPicker.selectedIndex];
    var selectedValue = eventPicker.value;
    var selectedText = selectedOption ? selectedOption.textContent.trim() : 'Select an event';
    var parent = eventPicker.parentElement;
    if (!parent) return;

    var label = parent.querySelector('label[for="eventPicker"]');

    var style = document.createElement('style');
    style.id = 'liveEventTreeStyles';
    style.textContent = `
        .live-event-tree-shell { position: relative; width: 100%; }
        .live-event-tree-trigger {
            width: 100%; min-height: 38px; display:flex; align-items:center; justify-content:space-between;
            gap:.75rem; text-align:left; background:#fff; border:1px solid #ced4da; border-radius:.375rem;
            padding:.45rem .75rem; color:#212529; transition:border-color .15s ease, box-shadow .15s ease;
        }
        .live-event-tree-trigger:hover, .live-event-tree-trigger:focus {
            border-color:var(--pv-green); outline:0; box-shadow:0 0 0 .2rem rgba(11,110,79,.12);
        }
        .live-event-tree-trigger .selected-text { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
        .live-event-tree-trigger .bi-chevron-down { flex-shrink:0; transition:transform .18s ease; }
        .live-event-tree-shell.open .live-event-tree-trigger .bi-chevron-down { transform:rotate(180deg); }
        .live-event-tree-panel {
            position:absolute; left:0; right:0; top:calc(100% + .35rem); z-index:1080; background:#fff;
            border:1px solid #d6dbdf; border-radius:.65rem; box-shadow:0 14px 34px rgba(22,37,32,.16);
            overflow:hidden;
        }
        .live-event-tree-panel[hidden] { display:none !important; }
        .live-event-tree-filter { padding:.75rem; border-bottom:1px solid #edf0f2; background:#f8faf9; }
        .live-event-tree-scroll { max-height:390px; overflow:auto; padding:.45rem; }
        .live-tree-node { border-radius:.45rem; }
        .live-tree-toggle, .live-tree-event {
            width:100%; border:0; background:transparent; display:flex; align-items:center; gap:.55rem;
            border-radius:.45rem; text-align:left; color:#1f2937;
        }
        .live-tree-toggle { padding:.55rem .6rem; font-weight:700; }
        .live-tree-toggle:hover, .live-tree-event:hover { background:#eef7f3; color:var(--pv-green-dark); }
        .live-tree-toggle .tree-chevron { width:1rem; flex-shrink:0; transition:transform .15s ease; }
        .live-tree-toggle.expanded .tree-chevron { transform:rotate(90deg); }
        .live-tree-count {
            margin-left:auto; min-width:1.65rem; text-align:center; border-radius:999px; padding:.1rem .42rem;
            background:#eef2f1; color:#5f6f69; font-size:.72rem; font-weight:700;
        }
        .live-tree-year-children { margin-left:.55rem; padding-left:.55rem; border-left:1px solid #e3e8e5; }
        .live-tree-month-children { margin-left:1.15rem; padding:.15rem 0 .35rem .7rem; border-left:1px solid #edf0ee; }
        .live-tree-date { color:#6c757d; font-size:.75rem; font-weight:800; text-transform:uppercase; letter-spacing:.025em; padding:.45rem .55rem .2rem; }
        .live-tree-event { padding:.5rem .55rem .5rem 1rem; font-size:.9rem; }
        .live-tree-event.selected { background:#e5f4ed; color:var(--pv-green-dark); font-weight:700; }
        .live-tree-event-title { flex:1; min-width:0; }
        .live-tree-type {
            flex-shrink:0; border-radius:999px; padding:.13rem .42rem; background:#f2f4f5;
            color:#66737b; font-size:.68rem; font-weight:700;
        }
        .live-tree-empty { padding:1.5rem .75rem; text-align:center; color:#7b858d; }
        @media (max-width: 575.98px) {
            .live-event-tree-panel { position:fixed; left:1rem; right:1rem; top:auto; bottom:1rem; max-height:72vh; }
            .live-event-tree-scroll { max-height:54vh; }
        }
    `;
    document.head.appendChild(style);

    var shell = document.createElement('div');
    shell.className = 'live-event-tree-shell';

    var trigger = document.createElement('button');
    trigger.type = 'button';
    trigger.id = 'liveEventTreeTrigger';
    trigger.className = 'live-event-tree-trigger';
    trigger.setAttribute('aria-haspopup', 'tree');
    trigger.setAttribute('aria-expanded', 'false');
    trigger.innerHTML = '<span class="selected-text"></span><i class="bi bi-chevron-down"></i>';
    trigger.querySelector('.selected-text').textContent = selectedText || 'Select an event';

    var panel = document.createElement('div');
    panel.className = 'live-event-tree-panel';
    panel.hidden = true;
    panel.innerHTML = `
        <div class="live-event-tree-filter">
            <label for="liveEventTypeFilter" class="form-label small fw-semibold mb-1">Filter by event type</label>
            <select id="liveEventTypeFilter" class="form-select form-select-sm">
                <option value="all">All event types</option>
            </select>
        </div>
        <div id="liveEventTreeScroll" class="live-event-tree-scroll">
            <div class="live-tree-empty"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Loading events...</div>
        </div>
    `;

    shell.appendChild(trigger);
    shell.appendChild(panel);
    eventPicker.insertAdjacentElement('afterend', shell);
    eventPicker.style.display = 'none';
    eventPicker.setAttribute('aria-hidden', 'true');
    eventPicker.tabIndex = -1;

    if (label) {
        label.setAttribute('for', trigger.id);
    }

    var treeScroll = panel.querySelector('#liveEventTreeScroll');
    var typeFilter = panel.querySelector('#liveEventTypeFilter');
    var monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];
    var expandedYears = new Set();
    var expandedMonths = new Set();
    var allEvents = [];

    function formatDate(dateString) {
        var parts = dateString.split('-');
        if (parts.length !== 3) return dateString;
        var date = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
        return date.toLocaleDateString('en-ZA', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    function openPanel() {
        panel.hidden = false;
        shell.classList.add('open');
        trigger.setAttribute('aria-expanded', 'true');
    }

    function closePanel() {
        panel.hidden = true;
        shell.classList.remove('open');
        trigger.setAttribute('aria-expanded', 'false');
    }

    function togglePanel() {
        if (panel.hidden) openPanel(); else closePanel();
    }

    function countEvents(monthMap) {
        var count = 0;
        monthMap.forEach(function (dateMap) {
            dateMap.forEach(function (events) {
                count += events.length;
            });
        });
        return count;
    }

    function createToggle(labelText, count, expanded, level) {
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'live-tree-toggle' + (expanded ? ' expanded' : '');
        button.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        button.dataset.level = level;
        button.innerHTML = '<i class="bi bi-chevron-right tree-chevron"></i><span class="tree-label"></span><span class="live-tree-count"></span>';
        button.querySelector('.tree-label').textContent = labelText;
        button.querySelector('.live-tree-count').textContent = count;
        return button;
    }

    function buildGroups(events) {
        var years = new Map();

        events.forEach(function (item) {
            var parts = item.date.split('-');
            if (parts.length !== 3) return;

            var year = Number(parts[0]);
            var month = Number(parts[1]) - 1;
            var dateKey = item.date;

            if (!years.has(year)) years.set(year, new Map());
            var months = years.get(year);
            if (!months.has(month)) months.set(month, new Map());
            var dates = months.get(month);
            if (!dates.has(dateKey)) dates.set(dateKey, []);
            dates.get(dateKey).push(item);
        });

        return years;
    }

    function renderTree() {
        var filterValue = typeFilter.value || 'all';
        var filtered = filterValue === 'all'
            ? allEvents.slice()
            : allEvents.filter(function (item) { return item.type === filterValue; });

        filtered.sort(function (a, b) {
            if (a.date !== b.date) return b.date.localeCompare(a.date);
            return a.title.localeCompare(b.title);
        });

        var years = buildGroups(filtered);
        treeScroll.innerHTML = '';

        if (!filtered.length) {
            treeScroll.innerHTML = '<div class="live-tree-empty"><i class="bi bi-calendar-x d-block fs-4 mb-2"></i>No events match this event type.</div>';
            return;
        }

        Array.from(years.keys()).sort(function (a, b) { return b - a; }).forEach(function (year) {
            var months = years.get(year);
            var yearExpanded = expandedYears.has(String(year));
            var yearNode = document.createElement('div');
            yearNode.className = 'live-tree-node';

            var yearButton = createToggle(String(year), countEvents(months), yearExpanded, 'year');
            var yearChildren = document.createElement('div');
            yearChildren.className = 'live-tree-year-children';
            yearChildren.hidden = !yearExpanded;

            yearButton.addEventListener('click', function () {
                var key = String(year);
                var willExpand = !expandedYears.has(key);
                if (willExpand) expandedYears.add(key); else expandedYears.delete(key);
                yearButton.classList.toggle('expanded', willExpand);
                yearButton.setAttribute('aria-expanded', willExpand ? 'true' : 'false');
                yearChildren.hidden = !willExpand;
            });

            Array.from(months.keys()).sort(function (a, b) { return b - a; }).forEach(function (month) {
                var dates = months.get(month);
                var monthKey = year + '-' + String(month + 1).padStart(2, '0');
                var monthExpanded = expandedMonths.has(monthKey);
                var monthNode = document.createElement('div');
                monthNode.className = 'live-tree-node';

                var monthCount = 0;
                dates.forEach(function (events) { monthCount += events.length; });

                var monthButton = createToggle(monthNames[month], monthCount, monthExpanded, 'month');
                var monthChildren = document.createElement('div');
                monthChildren.className = 'live-tree-month-children';
                monthChildren.hidden = !monthExpanded;

                monthButton.addEventListener('click', function () {
                    var willExpand = !expandedMonths.has(monthKey);
                    if (willExpand) expandedMonths.add(monthKey); else expandedMonths.delete(monthKey);
                    monthButton.classList.toggle('expanded', willExpand);
                    monthButton.setAttribute('aria-expanded', willExpand ? 'true' : 'false');
                    monthChildren.hidden = !willExpand;
                });

                Array.from(dates.keys()).sort(function (a, b) { return b.localeCompare(a); }).forEach(function (dateKey) {
                    var dateEvents = dates.get(dateKey);
                    var dateHeading = document.createElement('div');
                    dateHeading.className = 'live-tree-date';
                    dateHeading.textContent = formatDate(dateKey);
                    monthChildren.appendChild(dateHeading);

                    dateEvents.forEach(function (item) {
                        var eventButton = document.createElement('button');
                        eventButton.type = 'button';
                        eventButton.className = 'live-tree-event' + (String(item.id) === String(selectedValue) ? ' selected' : '');
                        eventButton.innerHTML = '<i class="bi bi-calendar-event"></i><span class="live-tree-event-title"></span><span class="live-tree-type"></span>';
                        eventButton.querySelector('.live-tree-event-title').textContent = item.title;
                        eventButton.querySelector('.live-tree-type').textContent = item.type;
                        eventButton.setAttribute('aria-current', String(item.id) === String(selectedValue) ? 'true' : 'false');

                        eventButton.addEventListener('click', function () {
                            eventPicker.value = String(item.id);
                            closePanel();
                            window.location.href = '/Attendance/Live?eventId=' + encodeURIComponent(item.id);
                        });

                        monthChildren.appendChild(eventButton);
                    });
                });

                monthNode.appendChild(monthButton);
                monthNode.appendChild(monthChildren);
                yearChildren.appendChild(monthNode);
            });

            yearNode.appendChild(yearButton);
            yearNode.appendChild(yearChildren);
            treeScroll.appendChild(yearNode);
        });
    }

    function expandSelectedPath() {
        var selected = allEvents.find(function (item) {
            return String(item.id) === String(selectedValue);
        });
        if (!selected) return;

        var parts = selected.date.split('-');
        if (parts.length !== 3) return;

        expandedYears.add(parts[0]);
        expandedMonths.add(parts[0] + '-' + parts[1]);
    }

    trigger.addEventListener('click', function () {
        togglePanel();
    });

    document.addEventListener('click', function (event) {
        if (!shell.contains(event.target)) closePanel();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !panel.hidden) {
            closePanel();
            trigger.focus();
        }
    });

    typeFilter.addEventListener('change', function () {
        sessionStorage.setItem('pv-live-event-type-filter', typeFilter.value);
        renderTree();
    });

    fetch('/Attendance/Live/EventPickerData', {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        credentials: 'same-origin'
    })
        .then(function (response) {
            if (!response.ok) throw new Error('Event picker request failed with HTTP ' + response.status + '.');
            return response.json();
        })
        .then(function (items) {
            allEvents = Array.isArray(items) ? items : [];

            var types = Array.from(new Set(allEvents.map(function (item) { return item.type; }).filter(Boolean))).sort();
            types.forEach(function (type) {
                var option = document.createElement('option');
                option.value = type;
                option.textContent = type;
                typeFilter.appendChild(option);
            });

            var savedFilter = sessionStorage.getItem('pv-live-event-type-filter');
            if (savedFilter && Array.from(typeFilter.options).some(function (option) { return option.value === savedFilter; })) {
                typeFilter.value = savedFilter;
            }

            expandSelectedPath();
            renderTree();
        })
        .catch(function (error) {
            console.error(error);
            treeScroll.innerHTML = '<div class="live-tree-empty"><i class="bi bi-exclamation-triangle d-block fs-4 mb-2"></i>The event list could not be loaded. Refresh the page and try again.</div>';
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
