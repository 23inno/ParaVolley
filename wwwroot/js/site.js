document.addEventListener('DOMContentLoaded', function () {
    var html = document.documentElement;
    if (html.getAttribute('data-theme') === 'system') {
        var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        html.setAttribute('data-theme', prefersDark ? 'dark' : 'light');
    }

    var toggleBtn = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('pvSidebar');

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('show');
        });
    }

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
