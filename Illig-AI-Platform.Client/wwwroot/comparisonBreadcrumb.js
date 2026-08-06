const states = new WeakMap();

function numberVariable(element, name, fallback) {
    const value = Number.parseFloat(getComputedStyle(element).getPropertyValue(name));
    return Number.isFinite(value) ? value : fallback;
}

function update(state) {
    state.frame = 0;

    const scrollRect = state.scrollElement.getBoundingClientRect();
    const headerHeight = numberVariable(
        state.scrollElement,
        "--su-comparison-table-head-height",
        48);
    const visibleTop = scrollRect.top + headerHeight;
    const breadcrumbRect = state.breadcrumbElement.hidden
        ? null
        : state.breadcrumbElement.getBoundingClientRect();
    const collisionBottom = breadcrumbRect
        ? Math.max(visibleTop, breadcrumbRect.bottom)
        : visibleTop;
    const rows = state.scrollElement.querySelectorAll("tr[data-comparison-breadcrumb]");
    let breadcrumb = "";
    let touchedGroupBreadcrumb = "";
    let firstVisibleRowFound = false;

    for (const row of rows) {
        const rowRect = row.getBoundingClientRect();
        const groupBreadcrumb = row.dataset.comparisonGroupBreadcrumb || "";

        if (groupBreadcrumb
            && rowRect.top <= collisionBottom
            && rowRect.bottom > visibleTop) {
            touchedGroupBreadcrumb = groupBreadcrumb;
        }

        if (!firstVisibleRowFound && rowRect.bottom > collisionBottom) {
            breadcrumb = row.dataset.comparisonBreadcrumb || "";
            firstVisibleRowFound = true;
        }
    }

    if (touchedGroupBreadcrumb)
        breadcrumb = touchedGroupBreadcrumb;

    if (state.breadcrumbElement.textContent !== breadcrumb)
        state.breadcrumbElement.textContent = breadcrumb;

    state.breadcrumbElement.hidden = breadcrumb.length === 0;
}

function schedule(state) {
    if (state.frame)
        return;

    state.frame = requestAnimationFrame(() => update(state));
}

export function initialize(scrollElement, breadcrumbElement) {
    if (!scrollElement || !breadcrumbElement)
        return;

    const existing = states.get(scrollElement);
    if (existing) {
        existing.breadcrumbElement = breadcrumbElement;
        schedule(existing);
        return;
    }

    const state = {
        scrollElement,
        breadcrumbElement,
        frame: 0,
        observer: null,
        onScroll: null
    };

    state.onScroll = () => schedule(state);
    state.observer = new MutationObserver(() => schedule(state));
    state.observer.observe(scrollElement, { childList: true, subtree: true });
    scrollElement.addEventListener("scroll", state.onScroll, { passive: true });
    states.set(scrollElement, state);
    schedule(state);
}

export function dispose(scrollElement) {
    const state = states.get(scrollElement);
    if (!state)
        return;

    state.scrollElement.removeEventListener("scroll", state.onScroll);
    state.observer.disconnect();
    if (state.frame)
        cancelAnimationFrame(state.frame);
    states.delete(scrollElement);
}
