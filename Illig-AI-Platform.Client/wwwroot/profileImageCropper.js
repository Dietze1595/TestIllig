(function () {
    const instances = new Map();

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    function constrain(state) {
        const width = state.image.naturalWidth * state.scale;
        const height = state.image.naturalHeight * state.scale;
        const maxX = Math.max(0, (width - state.canvas.width) / 2);
        const maxY = Math.max(0, (height - state.canvas.height) / 2);
        state.offsetX = clamp(state.offsetX, -maxX, maxX);
        state.offsetY = clamp(state.offsetY, -maxY, maxY);
    }

    function draw(state) {
        constrain(state);
        const canvas = state.canvas;
        const ctx = canvas.getContext("2d");
        const width = state.image.naturalWidth * state.scale;
        const height = state.image.naturalHeight * state.scale;
        const x = (canvas.width - width) / 2 + state.offsetX;
        const y = (canvas.height - height) / 2 + state.offsetY;

        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(state.image, x, y, width, height);
    }

    function changeZoom(state, zoom) {
        const oldScale = state.scale;
        state.zoom = clamp(Number(zoom), 1, 3);
        state.scale = state.baseScale * state.zoom;
        if (oldScale > 0) {
            const ratio = state.scale / oldScale;
            state.offsetX *= ratio;
            state.offsetY *= ratio;
        }
        draw(state);
    }

    window.profileImageCropper = {
        initialize: function (canvasId, sourceUrl) {
            const canvas = document.getElementById(canvasId);
            if (!canvas || !sourceUrl) {
                throw new Error("Cropper elements are unavailable.");
            }

            return new Promise((resolve, reject) => {
                const image = new Image();
                image.onload = function () {
                    const state = {
                        canvas,
                        image,
                        zoom: 1,
                        offsetX: 0,
                        offsetY: 0,
                        dragging: false,
                        pointerX: 0,
                        pointerY: 0
                    };
                    state.baseScale = Math.max(canvas.width / image.naturalWidth, canvas.height / image.naturalHeight);
                    state.scale = state.baseScale;
                    instances.set(canvasId, state);
                    draw(state);

                    canvas.onpointerdown = event => {
                        state.dragging = true;
                        state.pointerX = event.clientX;
                        state.pointerY = event.clientY;
                        canvas.setPointerCapture(event.pointerId);
                        canvas.classList.add("is-dragging");
                    };
                    canvas.onpointermove = event => {
                        if (!state.dragging) return;
                        const factorX = canvas.width / canvas.clientWidth;
                        const factorY = canvas.height / canvas.clientHeight;
                        state.offsetX += (event.clientX - state.pointerX) * factorX;
                        state.offsetY += (event.clientY - state.pointerY) * factorY;
                        state.pointerX = event.clientX;
                        state.pointerY = event.clientY;
                        draw(state);
                    };
                    canvas.onpointerup = canvas.onpointercancel = event => {
                        state.dragging = false;
                        canvas.classList.remove("is-dragging");
                        if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
                    };
                    canvas.onkeydown = event => {
                        const movement = event.shiftKey ? 10 : 2;
                        if (event.key === "ArrowLeft") state.offsetX -= movement;
                        else if (event.key === "ArrowRight") state.offsetX += movement;
                        else if (event.key === "ArrowUp") state.offsetY -= movement;
                        else if (event.key === "ArrowDown") state.offsetY += movement;
                        else return;
                        event.preventDefault();
                        draw(state);
                    };
                    resolve();
                };
                image.onerror = () => {
                    reject(new Error("Image could not be decoded."));
                };
                image.src = sourceUrl;
            });
        },

        setZoom: function (canvasId, zoom) {
            const state = instances.get(canvasId);
            if (state) changeZoom(state, zoom);
        },

        reset: function (canvasId) {
            const state = instances.get(canvasId);
            if (!state) return;
            state.offsetX = 0;
            state.offsetY = 0;
            changeZoom(state, 1);
        },

        exportImage: function (canvasId) {
            const state = instances.get(canvasId);
            if (!state) throw new Error("Cropper is not initialized.");

            const output = document.createElement("canvas");
            output.width = 512;
            output.height = 512;
            const ctx = output.getContext("2d");
            ctx.fillStyle = "#ffffff";
            ctx.fillRect(0, 0, output.width, output.height);
            ctx.drawImage(state.canvas, 0, 0, state.canvas.width, state.canvas.height, 0, 0, output.width, output.height);
            return output.toDataURL("image/jpeg", 0.9);
        }
    };
})();
