// Wandelt eine (potenziell sehr große) Base64-"data:"-URL in eine Blob-Object-URL um.
// Große data:-URLs (mehrere MB) werden vom Browser-PDF-Viewer im <iframe> nicht mehr
// geladen; Object-URLs haben keine praktische Längenbegrenzung. Siehe PdfViewerPanel.
window.pdfObjectUrl = {
    // Schneller Weg: Bytes per DotNetStreamReference (ohne Base64-Umweg) direkt zu einer
    // Blob-Object-URL. streamRef.arrayBuffer() überträgt die Rohbytes; kein ToBase64String
    // in .NET, kein atob/Schleife in JS.
    fromStream: async function (streamRef, typ) {
        if (!streamRef) {
            return null;
        }
        var buffer = await streamRef.arrayBuffer();
        return URL.createObjectURL(new Blob([buffer], { type: typ || "application/pdf" }));
    },
    toObjectUrl: function (dataUrl) {
        if (!dataUrl) {
            return null;
        }
        var komma = dataUrl.indexOf(",");
        if (komma < 0) {
            return null;
        }
        var meta = dataUrl.substring(0, komma);
        var base64 = dataUrl.substring(komma + 1);
        var binaer = atob(base64);
        var laenge = binaer.length;
        var bytes = new Uint8Array(laenge);
        for (var i = 0; i < laenge; i++) {
            bytes[i] = binaer.charCodeAt(i);
        }
        var treffer = /data:(.*?);base64/.exec(meta);
        var typ = treffer && treffer[1] ? treffer[1] : "application/pdf";
        return URL.createObjectURL(new Blob([bytes], { type: typ }));
    },
    revoke: function (objektUrl) {
        if (objektUrl) {
            try {
                URL.revokeObjectURL(objektUrl);
            } catch (e) {
                // Bereits widerrufen / ungültig — ignorieren.
            }
        }
    }
};
