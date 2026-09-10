// Sidebar min-height, empty; reserved for future interactivity.
// Currently the layout is static; keep this for progressive enhancement hooks.
document.addEventListener('DOMContentLoaded', () => {
    const banners = document.querySelectorAll('.banner');
    banners.forEach(b => {
        setTimeout(() => {
            b.classList.add('fade');
            setTimeout(() => b.remove(), 400);
        }, 5000);
    });
});