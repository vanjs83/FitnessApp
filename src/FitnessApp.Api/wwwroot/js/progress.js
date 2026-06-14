// Pose labels follow the same i18n-proxy pattern as ExerciseTypeLabels.
const ProgressPoseLabels = new Proxy({}, {
    get(_, pose) {
        const keys = {
            Front: 'progress.pose.front',
            Back: 'progress.pose.back',
            Side: 'progress.pose.side'
        };
        return keys[pose] ? I18n.t(keys[pose]) : pose;
    }
});

const Progress = {
    photos: [],

    init() {
        const uploadBtn = document.getElementById('progressUploadBtn');
        const fileInput = document.getElementById('progressFile');
        if (uploadBtn && fileInput) {
            uploadBtn.addEventListener('click', () => fileInput.click());
            fileInput.addEventListener('change', () => this.upload(fileInput.files[0]));
        }
        const dateInput = document.getElementById('progressTakenOn');
        if (dateInput && !dateInput.value) dateInput.value = new Date().toISOString().substring(0, 10);
    },

    async load() {
        const grid = document.getElementById('progressGallery');
        if (!grid) return;
        try {
            this.photos = await API.get('/progress');
            this.render(grid, this.photos, true);
        } catch (err) {
            grid.innerHTML = `<p class="muted small">${this.escape(err.message)}</p>`;
        }
    },

    async upload(file) {
        const msg = document.getElementById('progressUploadMsg');
        msg.textContent = '';
        msg.style.color = '';
        if (!file) return;

        if (file.size > 5 * 1024 * 1024) {
            msg.textContent = I18n.t('progress.imageTooLarge');
            return;
        }

        const form = new FormData();
        form.append('file', file);
        form.append('pose', document.getElementById('progressPose').value);
        form.append('takenOn', document.getElementById('progressTakenOn').value || new Date().toISOString().substring(0, 10));
        const note = document.getElementById('progressNote').value.trim();
        if (note) form.append('note', note);

        const token = API.getToken();
        try {
            const res = await fetch('/api/v1/progress', {
                method: 'POST',
                headers: token ? { 'Authorization': `Bearer ${token}` } : {},
                body: form
            });
            const text = await res.text();
            const data = text ? JSON.parse(text) : null;
            if (!res.ok) {
                throw new Error(data?.message || (data?.errors && Object.values(data.errors).flat().join(', ')) || `HTTP ${res.status}`);
            }
            document.getElementById('progressNote').value = '';
            msg.style.color = '#4dc878';
            msg.textContent = I18n.t('progress.saved');
            await this.load();
        } catch (err) {
            msg.textContent = err.message;
        } finally {
            document.getElementById('progressFile').value = '';
        }
    },

    async remove(id) {
        if (!confirm(I18n.t('progress.deleteConfirm'))) return;
        try {
            await API.delete(`/progress/${id}`);
            await this.load();
        } catch (err) {
            alert(err.message);
        }
    },

    // Renders the gallery grouped by pose. `editable` adds delete buttons (own gallery only).
    render(container, photos, editable) {
        if (!photos || !photos.length) {
            container.innerHTML = `<p class="muted small">${I18n.t('progress.empty')}</p>`;
            return;
        }
        const poses = ['Front', 'Back', 'Side'];
        const dateLocale = I18n.lang === 'en' ? 'en-GB' : 'hr-HR';

        container.innerHTML = poses.map(pose => {
            const group = photos.filter(p => p.pose === pose);
            if (!group.length) return '';
            const cards = group.map(p => {
                const date = new Date(p.takenOn).toLocaleDateString(dateLocale);
                const del = editable
                    ? `<button class="progress-del" title="${I18n.t('common.delete')}" onclick="Progress.remove(${p.id})">âś•</button>`
                    : '';
                const note = p.note ? `<div class="progress-note">${this.escape(p.note)}</div>` : '';
                return `
                    <figure class="progress-card">
                        ${del}
                        <a href="${this.escape(p.imageUrl)}" target="_blank" rel="noopener">
                            <img src="${this.escape(p.imageUrl)}" alt="${this.escape(date)}" loading="lazy">
                        </a>
                        <figcaption>${this.escape(date)}${note}</figcaption>
                    </figure>`;
            }).join('');
            return `
                <div class="progress-group">
                    <h4 class="progress-group-title">${ProgressPoseLabels[pose]}</h4>
                    <div class="progress-grid">${cards}</div>
                </div>`;
        }).join('');
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
