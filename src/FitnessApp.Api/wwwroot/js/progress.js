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
    _timer: null,

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

    // Renders the photos as an auto-rotating carousel ordered oldest -> newest so it reads as a
    // progression. `editable` adds a delete button on each slide (own gallery only).
    render(container, photos, editable) {
        if (this._timer) { clearInterval(this._timer); this._timer = null; }

        if (!photos || !photos.length) {
            container.innerHTML = `<p class="muted small">${I18n.t('progress.empty')}</p>`;
            return;
        }
        const dateLocale = I18n.lang === 'en' ? 'en-GB' : 'hr-HR';
        const ordered = [...photos].sort((a, b) => new Date(a.takenOn) - new Date(b.takenOn));

        const slides = ordered.map(p => {
            const date = new Date(p.takenOn).toLocaleDateString(dateLocale);
            const del = editable
                ? `<button class="progress-del" title="${I18n.t('common.delete')}" onclick="Progress.remove(${p.id})">&#10005;</button>`
                : '';
            const note = p.note ? `<div class="progress-note">${this.escape(p.note)}</div>` : '';
            return `
                <figure class="carousel-slide">
                    ${del}
                    <img src="${this.escape(p.imageUrl)}" alt="${this.escape(date)}" loading="lazy">
                    <figcaption><span class="carousel-pose">${ProgressPoseLabels[p.pose]}</span> · ${this.escape(date)}${note}</figcaption>
                </figure>`;
        }).join('');

        const controls = ordered.length > 1
            ? `<button class="carousel-arrow prev" aria-label="prev">&#8249;</button>
               <button class="carousel-arrow next" aria-label="next">&#8250;</button>
               <div class="carousel-dots">${ordered.map((_, i) =>
                   `<button class="carousel-dot${i === 0 ? ' active' : ''}" data-i="${i}" aria-label="${i + 1}"></button>`).join('')}</div>`
            : '';

        container.innerHTML = `
            <div class="carousel">
                <div class="carousel-track">${slides}</div>
                ${controls}
            </div>`;

        this.setupCarousel(container.querySelector('.carousel'), ordered.length);
    },

    // Wires arrows, dots, auto-rotation (4s) and pause-on-hover for the rendered carousel.
    setupCarousel(root, count) {
        if (!root || count <= 1) return;
        const track = root.querySelector('.carousel-track');
        const dots = [...root.querySelectorAll('.carousel-dot')];
        let index = 0;

        const go = i => {
            index = (i + count) % count;
            track.style.transform = `translateX(-${index * 100}%)`;
            dots.forEach((d, di) => d.classList.toggle('active', di === index));
        };
        const restart = () => {
            if (this._timer) clearInterval(this._timer);
            this._timer = setInterval(() => go(index + 1), 4000);
        };

        root.querySelector('.carousel-arrow.prev').addEventListener('click', () => { go(index - 1); restart(); });
        root.querySelector('.carousel-arrow.next').addEventListener('click', () => { go(index + 1); restart(); });
        dots.forEach(d => d.addEventListener('click', () => { go(Number(d.dataset.i)); restart(); }));
        root.addEventListener('mouseenter', () => { if (this._timer) clearInterval(this._timer); });
        root.addEventListener('mouseleave', restart);

        restart();
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
