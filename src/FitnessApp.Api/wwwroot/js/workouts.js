const Workouts = {
    list: [],
    current: null,
    page: 1,
    meta: null,

    init() {
        document.getElementById('newWorkoutBtn').addEventListener('click', () => {
            document.getElementById('newWorkoutForm').classList.remove('hidden');
        });
        document.getElementById('cancelWorkoutBtn').addEventListener('click', () => this.resetForm());
        document.getElementById('saveWorkoutBtn').addEventListener('click', () => this.save());
        document.getElementById('backToListBtn').addEventListener('click', () => this.showList());
        document.getElementById('addExerciseBtn').addEventListener('click', () => this.addExerciseToWorkout());
    },

    async load() {
        try {
            const res = await API.get(`/workouts?page=${this.page}`);
            if (this.page > 1 && res.totalPages > 0 && this.page > res.totalPages) {
                this.page = res.totalPages;
                return this.load();
            }
            this.list = res.items;
            this.meta = res;
            this.render();
        } catch (err) {
            console.error(err);
        }
    },

    renderPager() {
        Pagination.render(document.getElementById('workoutsListPager'), this.meta, p => {
            this.page = p;
            this.load();
        });
    },

    render() {
        const container = document.getElementById('workoutsList');
        if (this.list.length === 0) {
            container.innerHTML = `<p class="muted">${I18n.t('workouts.empty.client')}</p>`;
            this.renderPager();
            return;
        }

        container.innerHTML = this.list.map(w => `
            <div class="list-item" data-id="${w.id}">
                <div>
                    <h4>${this.escape(w.name)}</h4>
                    <div class="meta">
                        ${this.formatDate(w.performedAt)}
                        ${w.durationMinutes ? '· ' + w.durationMinutes + ' min' : ''}
                        · ${w.exerciseCount} ${I18n.t(w.exerciseCount === 1 ? 'workouts.exerciseUnit.one' : 'workouts.exerciseUnit.many')}
                    </div>
                </div>
                <span class="muted">→</span>
            </div>
        `).join('');

        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', () => this.showDetail(parseInt(el.dataset.id)));
        });

        this.renderPager();
    },

    async save() {
        const name = document.getElementById('workoutName').value.trim();
        if (!name) return;

        const dateInput = document.getElementById('workoutDate').value;
        const duration = document.getElementById('workoutDuration').value;

        try {
            await API.post('/workouts', {
                name,
                notes: document.getElementById('workoutNotes').value || null,
                performedAt: dateInput ? new Date(dateInput).toISOString() : null,
                durationMinutes: duration ? parseInt(duration) : null
            });
            this.resetForm();
            await this.load();
        } catch (err) {
            alert(err.message);
        }
    },

    resetForm() {
        document.getElementById('workoutName').value = '';
        document.getElementById('workoutDate').value = '';
        document.getElementById('workoutDuration').value = '';
        document.getElementById('workoutNotes').value = '';
        document.getElementById('newWorkoutForm').classList.add('hidden');
    },

    async showDetail(id) {
        try {
            this.current = await API.get(`/workouts/${id}`);
            document.getElementById('workoutsList').classList.add('hidden');
            document.getElementById('newWorkoutBtn').classList.add('hidden');
            document.querySelector('#workoutsView .view-header h2').textContent = '';
            document.getElementById('workoutDetail').classList.remove('hidden');

            document.getElementById('detailWorkoutName').textContent = this.current.name;
            document.getElementById('detailWorkoutMeta').textContent =
                `${this.formatDate(this.current.performedAt)}${this.current.durationMinutes ? ' · ' + this.current.durationMinutes + ' min' : ''}${this.current.notes ? ' · ' + this.current.notes : ''}`;

            this.renderExercises();
            await this.populateExerciseSelect();
        } catch (err) {
            alert(err.message);
        }
    },

    renderExercises() {
        const container = document.getElementById('exerciseList');
        if (!this.current.exercises.length) {
            container.innerHTML = `<p class="muted">${I18n.t('workouts.empty.day')}</p>`;
            return;
        }

        container.innerHTML = this.current.exercises.map((we, idx) => `
            <div class="exercise-block" data-we-id="${we.id}">
                <div class="row" style="justify-content: space-between; align-items: center;">
                    <h4>${this.escape(we.exerciseName)}</h4>
                    <span class="planned-actions">
                        <button class="move-we-btn" data-we-id="${we.id}" data-direction="up" ${idx === 0 ? 'disabled' : ''} title="${I18n.t('workouts.moveUp')}">▲</button>
                        <button class="move-we-btn" data-we-id="${we.id}" data-direction="down" ${idx === this.current.exercises.length - 1 ? 'disabled' : ''} title="${I18n.t('workouts.moveDown')}">▼</button>
                        <button class="btn-delete delete-we-btn" data-we-id="${we.id}" title="${I18n.t('workouts.deleteExerciseTitle')}">🗑</button>
                    </span>
                </div>
                ${we.sets.map(s => `
                    <div class="set-row">
                        <span class="set-num">#${s.setNumber}</span>
                        <span>${s.weight} kg</span>
                        <span>${s.reps} rep</span>
                        <button class="icon-btn delete-set-btn" data-set-id="${s.id}" title="${I18n.t('workouts.deleteSetTitle')}">×</button>
                    </div>
                `).join('')}
                <div class="add-set-form">
                    <input type="number" class="set-num-input" placeholder="#" value="${we.sets.length + 1}" min="1">
                    <input type="number" class="weight-input" placeholder="kg" step="0.5" min="0">
                    <input type="number" class="reps-input" placeholder="rep" min="0">
                    <button class="add-set-btn">+</button>
                </div>
            </div>
        `).join('');

        container.querySelectorAll('.add-set-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addSet(e.target.closest('.exercise-block')));
        });
        container.querySelectorAll('.delete-set-btn').forEach(btn => {
            btn.addEventListener('click', e => this.deleteSet(parseInt(e.target.dataset.setId)));
        });
        container.querySelectorAll('.move-we-btn').forEach(btn => {
            btn.addEventListener('click', () => this.moveExercise(parseInt(btn.dataset.weId), btn.dataset.direction));
        });
        container.querySelectorAll('.delete-we-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteExercise(parseInt(btn.dataset.weId)));
        });
    },

    async moveExercise(weId, direction) {
        try {
            await API.put(`/workouts/exercises/${weId}/move?direction=${direction}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteExercise(weId) {
        if (!confirm(I18n.t('workouts.deleteExerciseConfirm'))) return;
        try {
            await API.delete(`/workouts/exercises/${weId}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async populateExerciseSelect() {
        if (!Exercises.list.length) await Exercises.load();
        const select = document.getElementById('exerciseSelect');
        select.innerHTML = Exercises.list.map(e =>
            `<option value="${e.id}">${this.escape(e.name)}${e.muscleGroup ? ' (' + this.escape(e.muscleGroup) + ')' : ''}</option>`
        ).join('');
    },

    async addExerciseToWorkout() {
        const exerciseId = parseInt(document.getElementById('exerciseSelect').value);
        if (!exerciseId) return;

        try {
            await API.post(`/workouts/${this.current.id}/exercises`, {
                exerciseId,
                order: this.current.exercises.length + 1
            });
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async addSet(block) {
        const weId = parseInt(block.dataset.weId);
        const setNum = parseInt(block.querySelector('.set-num-input').value);
        const weight = parseFloat(block.querySelector('.weight-input').value);
        const reps = parseInt(block.querySelector('.reps-input').value);

        if (!setNum || isNaN(weight) || !reps) {
            alert(I18n.t('workouts.fillSetWarning'));
            return;
        }

        try {
            await API.post(`/workouts/${this.current.id}/sets`, {
                workoutExerciseId: weId,
                setNumber: setNum,
                weight,
                reps
            });
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteSet(setId) {
        if (!confirm(I18n.t('workouts.deleteSetConfirm'))) return;
        try {
            await API.delete(`/workouts/sets/${setId}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    showList() {
        document.getElementById('workoutDetail').classList.add('hidden');
        document.getElementById('workoutsList').classList.remove('hidden');
        document.getElementById('newWorkoutBtn').classList.remove('hidden');
        document.querySelector('#workoutsView .view-header h2').textContent = I18n.t('workouts.title');
        this.current = null;
        this.load();
    },

    formatDate(s) {
        const d = new Date(s);
        return d.toLocaleDateString(I18n.lang === 'en' ? 'en-GB' : 'hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
