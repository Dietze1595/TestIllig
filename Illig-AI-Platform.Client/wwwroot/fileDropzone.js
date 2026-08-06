window.suFileDropzone = (function () {
    function init(dropzone, input) {
        if (!dropzone || !input || dropzone.dataset.suDropzoneBound) {
            return;
        }

        dropzone.dataset.suDropzoneBound = "1";

        function isDisabled() {
            return input.disabled;
        }

        function stop(event) {
            event.preventDefault();
            event.stopPropagation();
        }

        function setDragging(active) {
            dropzone.classList.toggle("su-dropzone--dragging", active && !isDisabled());
        }

        function openPicker() {
            if (!isDisabled()) {
                input.click();
            }
        }

        function applyFiles(fileList) {
            if (!fileList || fileList.length === 0 || isDisabled()) {
                return;
            }

            try {
                input.files = fileList;
            } catch {
                if (!window.DataTransfer) {
                    return;
                }

                const transfer = new DataTransfer();
                Array.from(fileList).forEach(file => transfer.items.add(file));
                input.files = transfer.files;
            }

            input.dispatchEvent(new Event("change", { bubbles: true }));
        }

        dropzone.addEventListener("click", function (event) {
            if (event.target === input || isDisabled()) {
                return;
            }

            openPicker();
        });

        dropzone.addEventListener("keydown", function (event) {
            if (isDisabled()) {
                return;
            }

            if (event.key === "Enter" || event.key === " ") {
                stop(event);
                openPicker();
            }
        });

        ["dragenter", "dragover"].forEach(function (eventName) {
            dropzone.addEventListener(eventName, function (event) {
                stop(event);
                setDragging(true);
            });
        });

        dropzone.addEventListener("dragleave", function (event) {
            stop(event);

            if (!dropzone.contains(event.relatedTarget)) {
                setDragging(false);
            }
        });

        dropzone.addEventListener("drop", function (event) {
            stop(event);
            setDragging(false);
            applyFiles(event.dataTransfer.files);
        });
    }

    return { init };
})();
