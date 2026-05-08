function getSelectedIds() {
    return Array.from(document.querySelectorAll('.gallery-checkbox:checked'))
        .map(el => el.dataset.id);
}

function onCheckboxChange() {
    const ids = getSelectedIds();
    const toolbar = document.getElementById('bulk-toolbar');
    const countEl = document.getElementById('selected-count');

    if (ids.length > 0) {
        toolbar.style.removeProperty('display');
        toolbar.style.display = '';
    } else {
        toolbar.style.display = 'none !important';
        toolbar.setAttribute('style', 'display:none!important');
    }

    if (countEl) countEl.textContent = `${ids.length} selected`;
}

async function quickAction(id, action, btn) {
    btn.disabled = true;
    try {
        const resp = await fetch(`/review/${action}/${id}`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() }
        });
        if (resp.ok) {
            const card = btn.closest('.gallery-item');
            if (card) {
                card.style.transition = 'opacity .3s';
                card.style.opacity = '0';
                setTimeout(() => card.remove(), 300);
            }
        }
    } catch (e) {
        btn.disabled = false;
        console.error(e);
    }
}

async function bulkAction(action) {
    const ids = getSelectedIds();
    if (ids.length === 0) return;

    const tag = action === 'tag' ? document.getElementById('bulk-tag-input')?.value?.trim() : null;
    if (action === 'tag' && !tag) {
        alert('Enter a tag name first.');
        return;
    }

    const resp = await fetch('/review/bulk', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({ ids, action, tag })
    });

    if (resp.ok) {
        if (action !== 'tag') {
            // Remove affected cards
            ids.forEach(id => {
                const card = document.querySelector(`.gallery-item[data-id="${id}"]`);
                if (card) card.remove();
            });
        }
        document.querySelectorAll('.gallery-checkbox').forEach(cb => cb.checked = false);
        onCheckboxChange();
    }
}

async function bulkDownload() {
    const ids = getSelectedIds();
    if (ids.length === 0) return;

    const resp = await fetch('/download/bulk', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({ ids })
    });

    if (resp.ok) {
        const blob = await resp.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'pism-export.zip';
        a.click();
        URL.revokeObjectURL(url);
    }
}

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}
