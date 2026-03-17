window.bannerDropZone = {
    init: function (dropZoneId, dotnetRef) {
        const el = document.getElementById(dropZoneId);
        if (!el) return;

        // Remove previous listeners before re-attaching (called on every Blazor render)
        if (el._dropCleanup) el._dropCleanup();

        const onDragOver = e => {
            e.preventDefault();
            el.style.borderColor = '#1976d2';
        };

        const onDragLeave = () => {
            el.style.borderColor = '';
        };

        const onDrop = async e => {
            e.preventDefault();
            el.style.borderColor = '';

            const file = e.dataTransfer?.files?.[0];
            if (!file || !file.type.startsWith('image/')) return;

            // Blob URL is just a short string — safe for SignalR
            const previewUrl = URL.createObjectURL(file);

            // Upload via HTTP to bypass SignalR size limit
            const formData = new FormData();
            formData.append('file', file);
            const response = await fetch('/api/banner-upload', { method: 'POST', body: formData });
            if (!response.ok) return;
            const { tempFileName } = await response.json();

            await dotnetRef.invokeMethodAsync('OnFileDrop', file.name, tempFileName, previewUrl);
        };

        el.addEventListener('dragover', onDragOver);
        el.addEventListener('dragleave', onDragLeave);
        el.addEventListener('drop', onDrop);

        el._dropCleanup = () => {
            el.removeEventListener('dragover', onDragOver);
            el.removeEventListener('dragleave', onDragLeave);
            el.removeEventListener('drop', onDrop);
        };
    }
};
