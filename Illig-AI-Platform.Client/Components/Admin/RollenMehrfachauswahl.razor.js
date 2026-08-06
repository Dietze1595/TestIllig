const aussenklickHandler = new WeakMap();
const positionierungsHandler = new WeakMap();

function positionBerechnen(element) {
    const trigger = element.querySelector(".rollenauswahl__trigger");
    const panel = element.querySelector(".rollenauswahl__panel");
    if (!trigger || !panel) {
        return;
    }

    const abstand = 6;
    const fensterrand = 8;
    const triggerRect = trigger.getBoundingClientRect();

    panel.style.minWidth = `${triggerRect.width}px`;
    panel.style.maxHeight = "";

    const panelBreite = panel.offsetWidth;
    const panelHoehe = panel.offsetHeight;
    const platzUnten = window.innerHeight - triggerRect.bottom - abstand - fensterrand;
    const platzOben = triggerRect.top - abstand - fensterrand;
    const nachOben = panelHoehe > platzUnten && platzOben > platzUnten;
    const verfuegbareHoehe = Math.max(80, nachOben ? platzOben : platzUnten);

    panel.style.left = `${Math.max(
        fensterrand,
        Math.min(triggerRect.left, window.innerWidth - panelBreite - fensterrand)
    )}px`;
    panel.style.maxHeight = `${verfuegbareHoehe}px`;
    panel.style.top = nachOben
        ? `${Math.max(fensterrand, triggerRect.top - Math.min(panelHoehe, verfuegbareHoehe) - abstand)}px`
        : `${triggerRect.bottom + abstand}px`;
}

export function aussenklickRegistrieren(element, dotNetReferenz) {
    aussenklickEntfernen(element);

    const handler = event => {
        if (element.contains(event.target)) {
            return;
        }

        aussenklickEntfernen(element);
        dotNetReferenz.invokeMethodAsync("VonAussenSchliessen");
    };

    aussenklickHandler.set(element, handler);
    document.addEventListener("pointerdown", handler, true);
}

export function dropdownPositionieren(element) {
    positionBerechnen(element);

    const handler = () => positionBerechnen(element);
    positionierungsHandler.set(element, handler);
    window.addEventListener("resize", handler);
    document.addEventListener("scroll", handler, true);
}

export function aussenklickEntfernen(element) {
    const handler = aussenklickHandler.get(element);
    if (handler) {
        document.removeEventListener("pointerdown", handler, true);
        aussenklickHandler.delete(element);
    }

    const positionsHandler = positionierungsHandler.get(element);
    if (positionsHandler) {
        window.removeEventListener("resize", positionsHandler);
        document.removeEventListener("scroll", positionsHandler, true);
        positionierungsHandler.delete(element);
    }
}
