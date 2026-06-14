const ExerciseTypeLabels = new Proxy({}, {
    get(_, type) {
        const keys = {
            Strength: 'exercises.type.strength',
            HIIT: 'exercises.type.hiit',
            Kardio: 'exercises.type.kardio',
            Cicle: 'exercises.type.cicle',
            Funkcionalni: 'exercises.type.funkcionalni',
            FullBody: 'exercises.type.fullbody',
            Power: 'exercises.type.power'
        };
        return keys[type] ? I18n.t(keys[type]) : type;
    }
});

const Exercises = {
    list: [],
    search: '',
    typeFilter: '',
    current: null,

    init() {
        document.getElementById('newExerciseBtn').addEventListener('click', () => {
            document.getElementById('newExerciseForm').classList.remove('hidden');
        });
        document.getElementById('cancelExerciseBtn').addEventListener('click', () => this.resetForm());
        document.getElementById('saveExerciseBtn').addEventListener('click', () => this.save());

        document.getElementById('exercisesSearch').addEventListener('input', e => {
            this.search = e.target.value.toLowerCase();
            this.render();
        });

        document.getElementById('exercisesTypeFilter').addEventListener('change', e => {
            this.typeFilter = e.target.value;
            this.render();
        });

        document.getElementById('closeExerciseDetailBtn').addEventListener('click', () => this.closeDetail());
        document.getElementById('editExerciseBtn').addEventListener('click', () => this.startEdit());
        document.getElementById('cancelExerciseEditBtn').addEventListener('click', () => this.cancelEdit());
        document.getElementById('saveExerciseEditBtn').addEventListener('click', () => this.saveEdit());
    },

    async load() {
        try {
            this.list = await API.get('/exercises');
            this.render();
        } catch (err) {
            console.error(err);
        }
    },

    render() {
        const container = document.getElementById('exercisesList');
        const filtered = this.list.filter(e => {
            const matchesSearch = !this.search ||
                (e.name || '').toLowerCase().includes(this.search) ||
                (e.muscleGroup || '').toLowerCase().includes(this.search);
            const matchesType = !this.typeFilter || e.type === this.typeFilter;
            return matchesSearch && matchesType;
        });
        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${I18n.t(this.list.length === 0 ? 'exercises.empty.list' : 'exercises.empty.search')}</p>`;
            return;
        }

        container.innerHTML = filtered.map(e => `
            <div class="list-item exercise-item" data-id="${e.id}">
                ${this.thumbHtml(e)}
                <div>
                    <h4>${this.escape(e.name)}</h4>
                    <div class="meta">
                        ${e.muscleGroup ? this.escape(e.muscleGroup) : '—'}
                        · <span class="badge type-${(e.type || '').toLowerCase()}">${ExerciseTypeLabels[e.type] || e.type}</span>
                        ${e.videoUrl ? '· <span class="badge video">▶ video</span>' : ''}
                    </div>
                </div>
            </div>
        `).join('');

        container.querySelectorAll('.exercise-item').forEach(el => {
            el.addEventListener('click', () => this.showDetail(parseInt(el.dataset.id)));
        });
    },

    async save() {
        const name = document.getElementById('exerciseName').value.trim();
        if (!name) return;

        const fileInput = document.getElementById('exerciseVideoFile');
        const videoFile = fileInput && fileInput.files && fileInput.files[0];
        const imageInput = document.getElementById('exerciseImageFile');
        const imageFile = imageInput && imageInput.files && imageInput.files[0];

        try {
            const created = await API.post('/exercises', {
                name,
                muscleGroup: document.getElementById('exerciseMuscleGroup').value || null,
                description: document.getElementById('exerciseDescription').value || null,
                videoUrl: null,
                type: document.getElementById('exerciseType').value
            });
            if (videoFile && created && created.id) {
                try {
                    await this.uploadVideo(created.id, videoFile);
                } catch (uploadErr) {
                    alert(I18n.t('exercises.videoUploadFailed') + ' ' + uploadErr.message);
                }
            }
            if (imageFile && created && created.id) {
                try {
                    await this.uploadImage(created.id, imageFile);
                } catch (uploadErr) {
                    alert(I18n.t('exercises.imageUploadFailed') + ' ' + uploadErr.message);
                }
            }
            this.resetForm();
            await this.load();
        } catch (err) {
            alert(err.message);
        }
    },

    resetForm() {
        document.getElementById('exerciseName').value = '';
        document.getElementById('exerciseMuscleGroup').value = '';
        document.getElementById('exerciseDescription').value = '';
        const fileInput = document.getElementById('exerciseVideoFile');
        if (fileInput) fileInput.value = '';
        const imageInput = document.getElementById('exerciseImageFile');
        if (imageInput) imageInput.value = '';
        document.getElementById('exerciseType').value = 'Strength';
        document.getElementById('newExerciseForm').classList.add('hidden');
    },

    async uploadVideo(exerciseId, file) {
        const fd = new FormData();
        fd.append('file', file);
        const res = await fetch(`/api/v1/exercises/${exerciseId}/video`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
            body: fd
        });
        if (!res.ok) {
            let msg = 'Upload failed.';
            try { const j = await res.json(); if (j && j.message) msg = j.message; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    async deleteVideo(exerciseId) {
        const res = await fetch(`/api/v1/exercises/${exerciseId}/video`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        if (!res.ok) {
            let msg = 'Delete failed.';
            try { const j = await res.json(); if (j && j.message) msg = j.message; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    isLocalVideo(url) {
        return typeof url === 'string' && url.startsWith('/uploads/exercises/');
    },

    // Thumbnail rectangle for a list. Shows the image or a neutral placeholder.
    // sizeClass lets callers use a smaller variant in dense rows (e.g. plan editor).
    thumbHtml(e, sizeClass = 'exercise-thumb') {
        if (e.imageUrl) {
            return `<span class="${sizeClass}"><img src="${this.escape(e.imageUrl)}" alt="" loading="lazy"></span>`;
        }
        return `<span class="${sizeClass} exercise-thumb-empty">🏋️</span>`;
    },

    async uploadImage(exerciseId, file) {
        const fd = new FormData();
        fd.append('file', file);
        const res = await fetch(`/api/v1/exercises/${exerciseId}/image`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
            body: fd
        });
        if (!res.ok) {
            let msg = 'Upload failed.';
            try { const j = await res.json(); if (j && j.message) msg = j.message; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    async deleteImage(exerciseId) {
        const res = await fetch(`/api/v1/exercises/${exerciseId}/image`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        if (!res.ok) {
            let msg = 'Delete failed.';
            try { const j = await res.json(); if (j && j.message) msg = j.message; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    showDetail(id) {
        const e = this.list.find(x => x.id === id);
        if (!e) return;
        this.current = e;
        this.renderDetail();
        document.getElementById('exerciseDetailView').classList.remove('hidden');
        document.getElementById('exerciseEditView').classList.add('hidden');
        document.getElementById('exerciseDetailModal').classList.remove('hidden');
    },

    renderDetail() {
        const e = this.current;
        document.getElementById('exerciseDetailName').textContent = e.name;

        const metaParts = [];
        if (e.muscleGroup) metaParts.push(e.muscleGroup);
        metaParts.push(ExerciseTypeLabels[e.type] || e.type);
        document.getElementById('exerciseDetailMeta').textContent = metaParts.join(' · ');

        const imageWrap = document.getElementById('exerciseDetailImageWrap');
        if (imageWrap) {
            imageWrap.innerHTML = '';
            if (e.imageUrl) {
                const img = document.createElement('img');
                img.src = e.imageUrl;
                img.alt = e.name || '';
                imageWrap.appendChild(img);
                imageWrap.classList.remove('hidden');
            } else {
                imageWrap.classList.add('hidden');
            }
        }

        const videoWrap = document.getElementById('exerciseDetailVideoWrap');
        const noVideo = document.getElementById('exerciseDetailNoVideo');
        videoWrap.innerHTML = '';
        if (this.isLocalVideo(e.videoUrl)) {
            const v = document.createElement('video');
            v.src = e.videoUrl;
            v.controls = true;
            v.playsInline = true;
            v.style.width = '100%';
            v.style.borderRadius = '8px';
            videoWrap.appendChild(v);
            videoWrap.classList.remove('hidden');
            noVideo.classList.add('hidden');
        } else {
            const embed = this.toYouTubeEmbed(e.videoUrl);
            if (embed) {
                const iframe = document.createElement('iframe');
                iframe.src = embed;
                iframe.allowFullscreen = true;
                iframe.setAttribute('allow', 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture');
                videoWrap.appendChild(iframe);
                videoWrap.classList.remove('hidden');
                noVideo.classList.add('hidden');
            } else {
                videoWrap.classList.add('hidden');
                noVideo.classList.remove('hidden');
            }
        }

        document.getElementById('exerciseDetailDescription').textContent = e.description || '—';

        const editBtn = document.getElementById('editExerciseBtn');
        if (e.canEdit) editBtn.classList.remove('hidden');
        else editBtn.classList.add('hidden');
    },

    closeDetail() {
        const wrap = document.getElementById('exerciseDetailVideoWrap');
        if (wrap) wrap.innerHTML = '';
        document.getElementById('exerciseDetailModal').classList.add('hidden');
        this.current = null;
    },

    startEdit() {
        const e = this.current;
        if (!e || !e.canEdit) return;
        document.getElementById('editExerciseName').value = e.name || '';
        document.getElementById('editExerciseMuscleGroup').value = e.muscleGroup || '';
        document.getElementById('editExerciseType').value = e.type || 'Strength';
        const ytInput = document.getElementById('editExerciseVideoUrl');
        ytInput.value = this.isLocalVideo(e.videoUrl) ? '' : (e.videoUrl || '');
        const fileInput = document.getElementById('editExerciseVideoFile');
        if (fileInput) fileInput.value = '';
        this.renderEditVideoCurrent(e);
        const imageInput = document.getElementById('editExerciseImageFile');
        if (imageInput) imageInput.value = '';
        this.renderEditImageCurrent(e);
        document.getElementById('editExerciseDescription').value = e.description || '';
        document.getElementById('exerciseEditMsg').textContent = '';
        document.getElementById('exerciseDetailView').classList.add('hidden');
        document.getElementById('exerciseEditView').classList.remove('hidden');
    },

    renderEditVideoCurrent(e) {
        const slot = document.getElementById('editExerciseVideoCurrent');
        if (!slot) return;
        slot.innerHTML = '';
        if (!this.isLocalVideo(e.videoUrl)) return;
        const label = document.createElement('span');
        label.className = 'muted small';
        label.textContent = I18n.t('exercises.videoLoadedLabel');
        label.style.marginRight = '0.6rem';
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'secondary';
        removeBtn.textContent = I18n.t('profile.avatarRemove');
        removeBtn.addEventListener('click', async () => {
            if (!confirm(I18n.t('exercises.removeVideoConfirm'))) return;
            try {
                const updated = await this.deleteVideo(e.id);
                this.current = updated;
                const idx = this.list.findIndex(x => x.id === updated.id);
                if (idx >= 0) this.list[idx] = updated;
                this.renderEditVideoCurrent(updated);
            } catch (err) {
                document.getElementById('exerciseEditMsg').textContent = err.message;
            }
        });
        slot.appendChild(label);
        slot.appendChild(removeBtn);
    },

    renderEditImageCurrent(e) {
        const slot = document.getElementById('editExerciseImageCurrent');
        if (!slot) return;
        slot.innerHTML = '';
        if (!e.imageUrl) return;
        const thumb = document.createElement('img');
        thumb.src = e.imageUrl;
        thumb.alt = '';
        thumb.className = 'exercise-edit-thumb';
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'secondary';
        removeBtn.textContent = I18n.t('profile.avatarRemove');
        removeBtn.addEventListener('click', async () => {
            if (!confirm(I18n.t('exercises.removeImageConfirm'))) return;
            try {
                const updated = await this.deleteImage(e.id);
                this.current = updated;
                const idx = this.list.findIndex(x => x.id === updated.id);
                if (idx >= 0) this.list[idx] = updated;
                this.renderEditImageCurrent(updated);
                this.render();
            } catch (err) {
                document.getElementById('exerciseEditMsg').textContent = err.message;
            }
        });
        slot.appendChild(thumb);
        slot.appendChild(removeBtn);
    },

    cancelEdit() {
        document.getElementById('exerciseEditView').classList.add('hidden');
        document.getElementById('exerciseDetailView').classList.remove('hidden');
    },

    async saveEdit() {
        const e = this.current;
        if (!e) return;
        const name = document.getElementById('editExerciseName').value.trim();
        if (!name) {
            document.getElementById('exerciseEditMsg').textContent = 'Naziv je obavezan.';
            return;
        }

        const fileInput = document.getElementById('editExerciseVideoFile');
        const videoFile = fileInput && fileInput.files && fileInput.files[0];
        const imageInput = document.getElementById('editExerciseImageFile');
        const imageFile = imageInput && imageInput.files && imageInput.files[0];
        const ytLink = document.getElementById('editExerciseVideoUrl').value.trim();
        const keepLocal = this.isLocalVideo(e.videoUrl) && !videoFile && !ytLink;

        try {
            let updated = await API.put(`/exercises/${e.id}`, {
                name,
                muscleGroup: document.getElementById('editExerciseMuscleGroup').value || null,
                description: document.getElementById('editExerciseDescription').value || null,
                videoUrl: videoFile ? null : (ytLink ? ytLink : (keepLocal ? e.videoUrl : null)),
                type: document.getElementById('editExerciseType').value
            });
            if (videoFile) {
                try {
                    updated = await this.uploadVideo(e.id, videoFile);
                } catch (uploadErr) {
                    document.getElementById('exerciseEditMsg').textContent = 'Upload videa nije uspio: ' + uploadErr.message;
                    return;
                }
            }
            if (imageFile) {
                try {
                    updated = await this.uploadImage(e.id, imageFile);
                } catch (uploadErr) {
                    document.getElementById('exerciseEditMsg').textContent = I18n.t('exercises.imageUploadFailed') + ' ' + uploadErr.message;
                    return;
                }
            }
            this.current = updated;
            const idx = this.list.findIndex(x => x.id === updated.id);
            if (idx >= 0) this.list[idx] = updated;
            this.render();
            this.renderDetail();
            if (typeof Plans !== 'undefined' && Plans.currentPlan) {
                Plans.renderMyExercises();
                Plans.renderDays();
            }
            document.getElementById('exerciseEditView').classList.add('hidden');
            document.getElementById('exerciseDetailView').classList.remove('hidden');
        } catch (err) {
            document.getElementById('exerciseEditMsg').textContent = err.message;
        }
    },

    toYouTubeEmbed(url) {
        if (!url) return null;
        const u = url.trim();
        if (!u) return null;
        const patterns = [
            /(?:youtube\.com\/watch\?(?:.*&)?v=|youtu\.be\/|youtube\.com\/embed\/|youtube\.com\/shorts\/)([A-Za-z0-9_-]{6,})/
        ];
        for (const p of patterns) {
            const m = u.match(p);
            if (m) return `https://www.youtube.com/embed/${m[1]}`;
        }
        return null;
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
