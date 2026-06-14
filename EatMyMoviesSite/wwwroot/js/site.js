document.addEventListener('DOMContentLoaded', () => {
    const navbarBurgers = Array.from(document.querySelectorAll('.navbar-burger'));
    const listDropdowns = Array.from(document.querySelectorAll('.nav-list-dropdown'));

    const closeDropdowns = () => {
        listDropdowns.forEach((dropdown) => {
            dropdown.classList.remove('is-active');
            dropdown.querySelector('.nav-list-toggle')?.setAttribute('aria-expanded', 'false');
        });
    };

    const setBurgerState = (burger, target, isActive) => {
        burger.classList.toggle('is-active', isActive);
        target.classList.toggle('is-active', isActive);
        burger.setAttribute('aria-expanded', isActive.toString());
        burger.setAttribute('aria-label', isActive ? 'Close menu' : 'Open menu');

        if (!isActive) {
            closeDropdowns();
        }
    };

    navbarBurgers.forEach((burger) => {
        const targetId = burger.dataset.target;
        const target = targetId ? document.getElementById(targetId) : null;

        if (!target) {
            return;
        }

        burger.addEventListener('click', () => {
            setBurgerState(burger, target, !burger.classList.contains('is-active'));
        });

        burger.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                setBurgerState(burger, target, !burger.classList.contains('is-active'));
            }

            if (event.key === 'Escape') {
                setBurgerState(burger, target, false);
            }
        });
    });

    listDropdowns.forEach((dropdown) => {
        const toggle = dropdown.querySelector('.nav-list-toggle');

        if (!toggle) {
            return;
        }

        const toggleDropdown = (event) => {
            if (!window.matchMedia('(max-width: 1023px)').matches) {
                return;
            }

            event.preventDefault();
            const isActive = dropdown.classList.toggle('is-active');
            toggle.setAttribute('aria-expanded', isActive.toString());
        };

        toggle.addEventListener('click', toggleDropdown);
        toggle.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                toggleDropdown(event);
            }

            if (event.key === 'Escape') {
                dropdown.classList.remove('is-active');
                toggle.setAttribute('aria-expanded', 'false');
            }
        });
    });
});
