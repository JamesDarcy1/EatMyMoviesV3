document.addEventListener('DOMContentLoaded', () => {
    const navbarBurgers = Array.from(document.querySelectorAll('.navbar-burger'));

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
        });
    });
});
