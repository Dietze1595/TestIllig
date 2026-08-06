// Scrollt innerhalb des nächstgelegenen Inhaltsbereichs, ohne die Blazor-Route
// oder den URL-Hash zu verändern.
export function toId(elementId) {
    const target = document.getElementById(elementId);
    if (!target) return;

    const container = target.closest(".su-review__merkmale-scroll");
    if (!container) {
        target.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
    }

    const containerTop = container.getBoundingClientRect().top;
    const targetTop = target.getBoundingClientRect().top;
    container.scrollTo({
        top: container.scrollTop + targetTop - containerTop - 12,
        behavior: "smooth"
    });
}
