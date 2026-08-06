// Ziehbare Trennleiste für das rechte PDF-Panel (Stücklistenprüfung). Läuft komplett in JS,
// damit während des Ziehens kein Blazor-Interop pro Pixel nötig ist.
window.suPdfResize = (function () {
    function init(resizerSelector, panelSelector, reserveSelector) {
        const resizer = document.querySelector(resizerSelector);
        const panel = document.querySelector(panelSelector);
        const reserves = document.querySelectorAll(reserveSelector);
        if (!resizer || !panel || reserves.length === 0 || resizer.dataset.suResizeBound)
            return;
        resizer.dataset.suResizeBound = "1";

        const min = 320;
        let startX = 0;
        let startWidth = 0;

        function onMouseMove(e) {
            const max = Math.round(window.innerWidth * 0.75);
            const delta = startX - e.clientX;
            const width = Math.min(max, Math.max(min, startWidth + delta));
            panel.style.width = width + "px";
            reserves.forEach(reserve =>
                reserve.style.paddingRight = `calc(${width}px + var(--su-panel-gap, 0px))`);
        }

        function onMouseUp() {
            panel.classList.remove("su-review__pdf--resizing");
            reserves.forEach(reserve => reserve.classList.remove("su-main--resizing"));
            document.removeEventListener("mousemove", onMouseMove);
            document.removeEventListener("mouseup", onMouseUp);
        }

        resizer.addEventListener("mousedown", function (e) {
            startX = e.clientX;
            startWidth = panel.getBoundingClientRect().width;
            // Transition auf beiden Seiten abschalten, sonst hinkt der linke Bereich (.su-main)
            // dem sofort mitgezogenen PDF-Panel rechts hinterher.
            panel.classList.add("su-review__pdf--resizing");
            reserves.forEach(reserve => reserve.classList.add("su-main--resizing"));
            document.addEventListener("mousemove", onMouseMove);
            document.addEventListener("mouseup", onMouseUp);
            e.preventDefault();
        });
    }

    function reset(panelSelector, reserveSelector) {
        const panel = document.querySelector(panelSelector);
        const reserves = document.querySelectorAll(reserveSelector);
        if (panel) panel.style.width = "";
        reserves.forEach(reserve => reserve.style.paddingRight = "");
    }

    return { init, reset };
})();
