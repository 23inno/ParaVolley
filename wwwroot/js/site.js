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

/* =========================================
   PUBLIC PORTAL - ABOUT HERO SLIDESHOW
========================================= */

document.addEventListener("DOMContentLoaded", function () {

    const slider = document.querySelector(".about-hero-slider");

    // Stop if the slideshow is not on this page
    if (!slider) {
        return;
    }

    const slides = slider.querySelectorAll(".about-slide");
    const dots = slider.querySelectorAll(".about-slider-dot");
    const previousButton = slider.querySelector(".about-slider-prev");
    const nextButton = slider.querySelector(".about-slider-next");

    // Stop if no slides exist
    if (slides.length === 0) {
        return;
    }

    let currentSlide = 0;
    let slideTimer;

    // 5 seconds between slides
    const slideInterval = 5000;

    function showSlide(index) {

        if (index >= slides.length) {
            index = 0;
        }

        if (index < 0) {
            index = slides.length - 1;
        }

        slides.forEach(function (slide) {
            slide.classList.remove("active");
        });

        dots.forEach(function (dot) {
            dot.classList.remove("active");
        });

        slides[index].classList.add("active");

        if (dots[index]) {
            dots[index].classList.add("active");
        }

        currentSlide = index;
    }

    function nextSlide() {
        showSlide(currentSlide + 1);
    }

    function previousSlide() {
        showSlide(currentSlide - 1);
    }

    function startSlideshow() {

        clearInterval(slideTimer);

        slideTimer = setInterval(function () {
            nextSlide();
        }, slideInterval);
    }

    function restartSlideshow() {
        clearInterval(slideTimer);
        startSlideshow();
    }

    if (nextButton) {
        nextButton.addEventListener("click", function () {
            nextSlide();
            restartSlideshow();
        });
    }

    if (previousButton) {
        previousButton.addEventListener("click", function () {
            previousSlide();
            restartSlideshow();
        });
    }

    dots.forEach(function (dot, index) {
        dot.addEventListener("click", function () {
            showSlide(index);
            restartSlideshow();
        });
    });

    showSlide(0);
    startSlideshow();
});


/* =========================================
   PUBLIC PORTAL - PASSWORD VISIBILITY
========================================= */

document.addEventListener("DOMContentLoaded", function () {

    const passwordInput = document.getElementById("password");
    const togglePassword = document.getElementById("togglePassword");

    if (!passwordInput || !togglePassword) {
        return;
    }

    togglePassword.addEventListener("click", function () {

        const icon = togglePassword.querySelector("i");

        if (passwordInput.type === "password") {

            passwordInput.type = "text";

            if (icon) {
                icon.classList.remove("bi-eye");
                icon.classList.add("bi-eye-slash");
            }

            togglePassword.setAttribute(
                "aria-label",
                "Hide password"
            );
        }
        else {

            passwordInput.type = "password";

            if (icon) {
                icon.classList.remove("bi-eye-slash");
                icon.classList.add("bi-eye");
            }

            togglePassword.setAttribute(
                "aria-label",
                "Show password"
            );
        }
    });
});


/* =========================================
   PUBLIC PORTAL - BACK TO TOP BUTTON
========================================= */

document.addEventListener("DOMContentLoaded", function () {

    const backToTopButton = document.getElementById("backToTopBtn");

    if (!backToTopButton) {
        return;
    }

    function toggleBackToTopButton() {

        if (window.scrollY > 350) {
            backToTopButton.classList.add("show");
        }
        else {
            backToTopButton.classList.remove("show");
        }
    }

    window.addEventListener(
        "scroll",
        toggleBackToTopButton,
        { passive: true }
    );

    backToTopButton.addEventListener("click", function () {

        window.scrollTo({
            top: 0,
            behavior: "smooth"
        });
    });

    toggleBackToTopButton();
});

// =========================================================
// CONTACT FORM - TEMPORARY FRONT-END ONLY
// Backend implementation will be added later.
// =========================================================
document.addEventListener('DOMContentLoaded', function () {

    var contactForm = document.getElementById('contactFakeForm');

    if (!contactForm) {
        return;
    }

    contactForm.addEventListener('submit', function (event) {

        event.preventDefault();

        var submitButton = contactForm.querySelector('button[type="submit"]');

        if (!submitButton) {
            return;
        }

        var originalHtml = submitButton.innerHTML;

        submitButton.disabled = true;

        submitButton.innerHTML =
            '<i class="bi bi-check-circle-fill me-2"></i> Message Ready';

        setTimeout(function () {

            submitButton.disabled = false;
            submitButton.innerHTML = originalHtml;

        }, 2500);

    });

});

// =========================================================
// PLAYER REGISTRATION - TEMPORARY FRONT-END ONLY
// Backend/database implementation can be connected later.
// =========================================================
document.addEventListener('DOMContentLoaded', function () {

    var registrationForm =
        document.getElementById('playerRegistrationFakeForm');

    if (!registrationForm) {
        return;
    }

    registrationForm.addEventListener('submit', function (event) {

        event.preventDefault();

        if (!registrationForm.checkValidity()) {
            registrationForm.reportValidity();
            return;
        }

        var submitButton =
            registrationForm.querySelector('button[type="submit"]');

        if (!submitButton) {
            return;
        }

        var originalHtml = submitButton.innerHTML;

        submitButton.disabled = true;

        submitButton.innerHTML =
            '<i class="bi bi-check-circle-fill"></i> Registration Ready';

        setTimeout(function () {

            submitButton.disabled = false;
            submitButton.innerHTML = originalHtml;

        }, 3000);

    });

});