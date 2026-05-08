let cropper = null;
let cropEnabled = false;
const imgEl = document.getElementById('main-image');

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

document.getElementById('btn-crop-toggle').addEventListener('click', function () {
    if (!cropEnabled) {
        enableCrop();
    } else {
        disableCrop();
    }
});

function enableCrop() {
    cropEnabled = true;
    document.getElementById('btn-crop-toggle').textContent = 'Cancel Crop';
    document.getElementById('btn-reset-crop').style.display = '';

    cropper = new Cropper(imgEl, {
        viewMode: 1,
        autoCropArea: 1,
        movable: false,
        zoomable: false,
        data: savedCrop || undefined
    });
}

function disableCrop() {
    cropEnabled = false;
    document.getElementById('btn-crop-toggle').textContent = 'Enable Crop';
    document.getElementById('btn-reset-crop').style.display = 'none';

    if (cropper) {
        cropper.destroy();
        cropper = null;
    }
}

document.getElementById('btn-reset-crop').addEventListener('click', function () {
    if (cropper) cropper.reset();
});

function rotateLeft() {
    currentRotation = ((currentRotation - 90) + 360) % 360;
    applyRotation();
}

function rotateRight() {
    currentRotation = (currentRotation + 90) % 360;
    applyRotation();
}

function applyRotation() {
    const rotDisplay = document.getElementById('rotation-display');
    if (rotDisplay) rotDisplay.textContent = currentRotation + '°';

    if (cropper) {
        cropper.rotateTo(0); // reset internal rotation
    }

    // Reload the preview with new rotation applied server-side
    const base = `/image/${IMAGE_ID}?mode=preview&r=${currentRotation}`;
    if (cropper) {
        const data = cropper.getData();
        savedCrop = (data.width > 0 && data.height > 0) ? data : null;
        disableCrop();
        imgEl.src = base;
    } else {
        imgEl.src = base;
    }
}

async function saveEdits() {
    let cropData = null;
    if (cropper) {
        const d = cropper.getData(true); // rounded pixels
        if (d.width > 0 && d.height > 0) {
            cropData = { x: d.x, y: d.y, width: d.width, height: d.height };
        }
    } else if (savedCrop) {
        cropData = savedCrop;
    }

    const payload = {
        rotation: currentRotation,
        x: cropData?.x ?? null,
        y: cropData?.y ?? null,
        width: cropData?.width ?? null,
        height: cropData?.height ?? null
    };

    const resp = await fetch(`/review/save-crop/${IMAGE_ID}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify(payload)
    });

    if (resp.ok) {
        savedCrop = cropData;
        const indicator = document.getElementById('edits-saved');
        if (indicator) {
            indicator.style.display = '';
            setTimeout(() => indicator.style.display = 'none', 2000);
        }
    }
}

async function reviewAction(action) {
    const resp = await fetch(`/review/${action}/${IMAGE_ID}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    });

    if (resp.ok) {
        const badge = document.getElementById('status-badge');
        if (badge) {
            const label = action === 'keep' ? 'Kept' : 'Deleted';
            badge.textContent = label;
            badge.className = `badge ${action === 'keep' ? 'bg-success' : 'bg-danger'}`;
        }

        // Navigate to next image if available
        const nextLink = document.querySelector(`a[href*="detail/"][href*="status=${STATUS_PARAM}"]`);
        if (nextLink && nextLink.textContent.includes('Next')) {
            window.location.href = nextLink.href;
        }
    }
}

async function addTag() {
    const input = document.getElementById('new-tag-input');
    const tag = input.value.trim();
    if (!tag) return;

    const resp = await fetch(`/review/add-tag/${IMAGE_ID}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({ tag })
    });

    if (resp.ok) {
        const data = await resp.json();
        renderTags(data.tags);
        input.value = '';
    }
}

async function removeTag(tagId, btn) {
    const resp = await fetch(`/review/remove-tag/${tagId}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    });

    if (resp.ok) {
        const badge = btn.closest('[data-tag-id]');
        if (badge) badge.remove();
    }
}

function renderTags(tags) {
    const list = document.getElementById('tag-list');
    if (!list) return;
    list.innerHTML = tags.map(t =>
        `<span class="badge bg-info text-dark d-flex align-items-center gap-1" data-tag-id="${t.id}">
            ${escapeHtml(t.tag)}
            <button type="button" class="btn-close btn-close-white" style="font-size:.5rem"
                onclick="removeTag('${t.id}', this)"></button>
        </span>`
    ).join('');
}

function escapeHtml(s) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
