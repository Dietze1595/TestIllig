(function () {
    const speicherSchluessel = "illig-theme";
    const erlaubteThemes = new Set(["dark", "light"]);

    function gespeichertesTheme() {
        try {
            const theme = window.localStorage.getItem(speicherSchluessel);
            return erlaubteThemes.has(theme) ? theme : null;
        } catch {
            return null;
        }
    }

    function systemTheme() {
        return window.matchMedia?.("(prefers-color-scheme: light)").matches ? "light" : "dark";
    }

    function anwenden(theme, speichern) {
        const gewaehlt = erlaubteThemes.has(theme) ? theme : "dark";
        document.documentElement.dataset.bsTheme = gewaehlt;
        document.documentElement.style.colorScheme = gewaehlt;

        if (speichern) {
            try {
                window.localStorage.setItem(speicherSchluessel, gewaehlt);
            } catch {
                // Der Modus gilt weiterhin für die aktuelle Seite.
            }
        }

        return gewaehlt;
    }

    const initial = anwenden(gespeichertesTheme() ?? systemTheme(), false);

    window.illigTheme = {
        get: () => document.documentElement.dataset.bsTheme ?? initial,
        set: theme => anwenden(theme, true),
        toggle: () => anwenden(
            document.documentElement.dataset.bsTheme === "light" ? "dark" : "light",
            true)
    };
})();
