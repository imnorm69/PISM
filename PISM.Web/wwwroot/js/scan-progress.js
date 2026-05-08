(function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/scan')
        .withAutomaticReconnect()
        .build();

    let activeJobs = {};

    connection.on('ScanProgressUpdated', function (update) {
        activeJobs[update.jobId] = update;
        updateNavStatus();
        updateJobCard(update);
    });

    connection.on('ScanJobCompleted', function (jobId) {
        delete activeJobs[jobId];
        updateNavStatus();
        markJobDone(jobId, 'Completed');
    });

    connection.on('ScanJobFailed', function (jobId, errorMessage) {
        delete activeJobs[jobId];
        updateNavStatus();
        markJobDone(jobId, 'Failed', errorMessage);
    });

    connection.start().catch(function (err) {
        console.warn('SignalR connection failed:', err);
    });

    function updateNavStatus() {
        const keys = Object.keys(activeJobs);
        const bar = document.getElementById('scan-progress-bar');
        const navStatus = document.getElementById('nav-scan-status');
        const navText = document.getElementById('nav-scan-text');

        if (keys.length === 0) {
            if (bar) bar.style.display = 'none';
            if (navStatus) navStatus.style.display = 'none';
            return;
        }

        if (bar) bar.style.display = 'block';
        if (navStatus) navStatus.style.display = '';

        // Show progress for the first active job
        const first = activeJobs[keys[0]];
        if (first && first.totalFiles > 0) {
            const pct = Math.round(first.processedFiles * 100 / first.totalFiles);
            const inner = document.getElementById('scan-progress-inner');
            if (inner) inner.style.width = pct + '%';
        }

        if (navText) {
            const label = keys.length === 1
                ? (first.currentFileName ? `Scanning: ${first.currentFileName}` : `Scanning…`)
                : `Scanning (${keys.length} jobs)…`;
            navText.textContent = label;
        }
    }

    function updateJobCard(update) {
        const card = document.querySelector(`.scan-job-card[data-job-id="${update.jobId}"]`);
        if (!card) return;

        const statusEl = card.querySelector('.job-status');
        if (statusEl) statusEl.textContent = update.status;

        const bar = card.querySelector('.job-progress-bar');
        if (bar && update.totalFiles > 0) {
            bar.style.width = Math.round(update.processedFiles * 100 / update.totalFiles) + '%';
        }

        const statsEl = card.querySelector('.job-stats');
        if (statsEl) {
            statsEl.textContent =
                `${update.processedFiles} / ${update.totalFiles} files — ` +
                `${update.newFiles} new, ${update.duplicatesFound} duplicates` +
                (update.currentFileName ? ` — ${update.currentFileName}` : '');
        }
    }

    function markJobDone(jobId, status, error) {
        const card = document.querySelector(`.scan-job-card[data-job-id="${jobId}"]`);
        if (!card) return;

        const statusEl = card.querySelector('.job-status');
        if (statusEl) {
            statusEl.textContent = status;
            statusEl.className = `badge ${status === 'Completed' ? 'bg-success' : 'bg-danger'} job-status`;
        }

        if (error) {
            const statsEl = card.querySelector('.job-stats');
            if (statsEl) statsEl.textContent += ` — Error: ${error}`;
        }
    }
})();
