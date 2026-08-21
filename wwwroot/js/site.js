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
        if (toggleBtn) {
            toggleBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    }

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function () {
            setMobileMenu(!sidebar.classList.contains('show'));
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            setMobileMenu(false);
        });
    }

    if (sidebar) {
        sidebar.querySelectorAll('nav a').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.matchMedia('(max-width: 991.98px)').matches) {
                    setMobileMenu(false);
                }
            });
        });
    }

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            setMobileMenu(false);
        }
    });

    // Desktop sidebar collapse/expand toggle
    var collapseBtn = document.getElementById('sidebarCollapseBtn');
    if (collapseBtn && sidebar) {
        collapseBtn.addEventListener('click', function () {
            var isCollapsed = sidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed ? 'true' : 'false');
            collapseBtn.setAttribute('title', isCollapsed ? 'Expand menu' : 'Collapse menu');
        });

        // Make sure the button's title matches the state applied by the
        // anti-flash inline script that ran before this file loaded.
        if (sidebar.classList.contains('collapsed')) {
            collapseBtn.setAttribute('title', 'Expand menu');
        }
    }

    // Auto-dismiss success alerts after a few seconds
    var alerts = document.querySelectorAll('.alert-auto-dismiss');
    alerts.forEach(function (alertEl) {
        setTimeout(function () {
            alertEl.classList.remove('show');
            alertEl.classList.add('fade');
        }, 4000);
    });
});
