document.addEventListener('DOMContentLoaded', () => {
    const navbarBurgers = Array.from(document.querySelectorAll('.navbar-burger'));
    const listDropdowns = Array.from(document.querySelectorAll('.nav-list-dropdown'));

    navbarBurgers.forEach((burger) => {
        const targetId = burger.dataset.target;
        const target = targetId ? document.getElementById(targetId) : null;

        if (!target) {
            return;
        }

        burger.addEventListener('click', () => {
            const isActive = burger.classList.toggle('is-active');
            target.classList.toggle('is-active', isActive);
            burger.setAttribute('aria-expanded', isActive.toString());

            if (!isActive) {
                listDropdowns.forEach((dropdown) => {
                    dropdown.classList.remove('is-active');
                    dropdown.querySelector('.nav-list-toggle')?.setAttribute('aria-expanded', 'false');
                });
            }
        });
    });

    listDropdowns.forEach((dropdown) => {
        const toggle = dropdown.querySelector('.nav-list-toggle');

        if (!toggle) {
            return;
        }

        toggle.addEventListener('click', (event) => {
            if (!window.matchMedia('(max-width: 1023px)').matches) {
                return;
            }

            event.preventDefault();
            const isActive = dropdown.classList.toggle('is-active');
            toggle.setAttribute('aria-expanded', isActive.toString());
        });
    });
});
