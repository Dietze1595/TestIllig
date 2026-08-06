const aussenklickHandler = new WeakMap();

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

export function aussenklickEntfernen(element) {
    const handler = aussenklickHandler.get(element);
    if (!handler) {
        return;
    }

    document.removeEventListener("pointerdown", handler, true);
    aussenklickHandler.delete(element);
}
