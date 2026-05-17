const ExerciseTypeLabels = {
    Strength: 'Snaga',
    HIIT: 'HIIT',
    Kardio: 'Kardio',
    Cicle: 'Cicle',
    Funkcionalni: 'Funkcionalni',
    FullBody: 'Full Body',
    Power: 'Power'
};

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
            container.innerHTML = `<p class="muted">${this.list.length === 0 ? 'Nema vježbi.' : 'Nema rezultata za pretragu.'}</p>`;
            return;
        }

        container.innerHTML = filtered.map(e => `
            <div class="list-item exercise-item" data-id="${e.id}">
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

        try {
            await API.post('/exercises', {
                name,
                muscleGroup: document.getElementById('exerciseMuscleGroup').value || null,
                description: document.getElementById('exerciseDescription').value || null,
                videoUrl: document.getElementById('exerciseVideoUrl').value || null,
                type: document.getElementById('exerciseType').value
            });
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
        document.getElementById('exerciseVideoUrl').value = '';
        document.getElementById('exerciseType').value = 'Strength';
        document.getElementById('newExerciseForm').classList.add('hidden');
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

        const videoWrap = document.getElementById('exerciseDetailVideoWrap');
        const videoFrame = document.getElementById('exerciseDetailVideo');
        const noVideo = document.getElementById('exerciseDetailNoVideo');
        const embed = this.toYouTubeEmbed(e.videoUrl);
        if (embed) {
            videoFrame.src = embed;
            videoWrap.classList.remove('hidden');
            noVideo.classList.add('hidden');
        } else {
            videoFrame.src = '';
            videoWrap.classList.add('hidden');
            noVideo.classList.remove('hidden');
        }

        document.getElementById('exerciseDetailDescription').textContent = e.description || '—';

        const editBtn = document.getElementById('editExerciseBtn');
        if (e.canEdit) editBtn.classList.remove('hidden');
        else editBtn.classList.add('hidden');
    },

    closeDetail() {
        document.getElementById('exerciseDetailVideo').src = '';
        document.getElementById('exerciseDetailModal').classList.add('hidden');
        this.current = null;
    },

    startEdit() {
        const e = this.current;
        if (!e || !e.canEdit) return;
        document.getElementById('editExerciseName').value = e.name || '';
        document.getElementById('editExerciseMuscleGroup').value = e.muscleGroup || '';
        document.getElementById('editExerciseType').value = e.type || 'Strength';
        document.getElementById('editExerciseVideoUrl').value = e.videoUrl || '';
        document.getElementById('editExerciseDescription').value = e.description || '';
        document.getElementById('exerciseEditMsg').textContent = '';
        document.getElementById('exerciseDetailView').classList.add('hidden');
        document.getElementById('exerciseEditView').classList.remove('hidden');
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

        try {
            const updated = await API.put(`/exercises/${e.id}`, {
                name,
                muscleGroup: document.getElementById('editExerciseMuscleGroup').value || null,
                description: document.getElementById('editExerciseDescription').value || null,
                videoUrl: document.getElementById('editExerciseVideoUrl').value || null,
                type: document.getElementById('editExerciseType').value
            });
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
