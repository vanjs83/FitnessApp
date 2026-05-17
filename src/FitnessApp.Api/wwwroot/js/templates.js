const Templates = {
    list: [],
    nutritionList: [],
    current: null,
    activeType: 'training',  // 'training' or 'nutrition'
    dayNames: {
        Sunday: 'Nedjelja', Monday: 'Ponedjeljak', Tuesday: 'Utorak',
        Wednesday: 'Srijeda', Thursday: 'Četvrtak', Friday: 'Petak', Saturday: 'Subota',
        0: 'Nedjelja', 1: 'Ponedjeljak', 2: 'Utorak',
        3: 'Srijeda', 4: 'Četvrtak', 5: 'Petak', 6: 'Subota'
    },

    init() {
        const newBtn = document.getElementById('newTemplateBtn');
        if (newBtn) newBtn.addEventListener('click', () => this.showNewForm());

        const saveBtn = document.getElementById('saveTemplateBtn');
        if (saveBtn) saveBtn.addEventListener('click', () => this.create());

        const cancelBtn = document.getElementById('cancelTemplateBtn');
        if (cancelBtn) cancelBtn.addEventListener('click', () => this.resetNewForm());

        const backBtn = document.getElementById('backToTemplatesBtn');
        if (backBtn) backBtn.addEventListener('click', () => this.showList());

        const editBtn = document.getElementById('editTemplateBtn');
        if (editBtn) editBtn.addEventListener('click', () => this.editCurrent());

        const delBtn = document.getElementById('deleteTemplateBtn');
        if (delBtn) delBtn.addEventListener('click', () => this.deleteCurrent());

        const addDayBtn = document.getElementById('addTemplateDayBtn');
        if (addDayBtn) addDayBtn.addEventListener('click', () => this.addDay());

        // Type toggle (training / nutrition)
        document.querySelectorAll('.tpl-type-tab').forEach(btn => {
            btn.addEventListener('click', () => this.switchType(btn.dataset.tplType));
        });

        // Nutrition template form handlers
        const saveNutBtn = document.getElementById('saveNutritionTemplateBtn');
        if (saveNutBtn) saveNutBtn.addEventListener('click', () => this.createNutritionTemplate());
        const cancelNutBtn = document.getElementById('cancelNutritionTemplateBtn');
        if (cancelNutBtn) cancelNutBtn.addEventListener('click', () =>
            document.getElementById('newNutritionTemplateForm').classList.add('hidden'));
    },

    switchType(type) {
        this.activeType = type;
        document.querySelectorAll('.tpl-type-tab').forEach(b =>
            b.classList.toggle('active', b.dataset.tplType === type));
        document.getElementById('templatesList').classList.toggle('hidden', type !== 'training');
        document.getElementById('nutritionTemplatesList').classList.toggle('hidden', type !== 'nutrition');
        // hide any open create form
        document.getElementById('newTemplateForm').classList.add('hidden');
        document.getElementById('newNutritionTemplateForm').classList.add('hidden');
        // close training template detail if open
        const tplDetail = document.getElementById('templateDetail');
        if (tplDetail) tplDetail.classList.add('hidden');
        // refresh the button label so user knows which type they create
        const newBtn = document.getElementById('newTemplateBtn');
        if (newBtn) {
            newBtn.classList.remove('hidden');
            newBtn.textContent = type === 'nutrition' ? '+ Novi predložak prehrane' : '+ Novi predložak treninga';
        }
        this.current = null;
        this.load();
    },

    showNewForm() {
        if (this.activeType === 'nutrition') {
            document.getElementById('nutritionTemplateName').value = '';
            document.getElementById('nutritionTemplateNotes').value = '';
            document.getElementById('newNutritionTemplateError').textContent = '';
            document.getElementById('newNutritionTemplateForm').classList.remove('hidden');
            document.getElementById('newTemplateForm').classList.add('hidden');
        } else {
            document.getElementById('templateName').value = '';
            document.getElementById('templateExpectations').value = '';
            document.getElementById('newTemplateError').textContent = '';
            document.getElementById('newTemplateForm').classList.remove('hidden');
            document.getElementById('newNutritionTemplateForm').classList.add('hidden');
        }
    },

    async createNutritionTemplate() {
        const errorEl = document.getElementById('newNutritionTemplateError');
        errorEl.textContent = '';
        const name = document.getElementById('nutritionTemplateName').value.trim();
        const notes = document.getElementById('nutritionTemplateNotes').value.trim();
        if (!name) { errorEl.textContent = 'Naziv je obavezan.'; return; }
        try {
            const tpl = await API.post('/nutrition-plans/templates', { name, notes: notes || null });
            document.getElementById('newNutritionTemplateForm').classList.add('hidden');
            await this.load();
            // Open in nutrition editor — switch to nutrition view
            App.switchTrainerView('nutrition');
            await Nutrition.showDetail(tpl.id);
        } catch (err) { errorEl.textContent = err.message; }
    },

    async load() {
        try {
            if (this.activeType === 'training') {
                this.list = await API.get('/training-plans/templates');
                this.render();
            } else {
                this.nutritionList = await API.get('/nutrition-plans/templates');
                this.renderNutrition();
            }
        } catch (err) {
            console.error(err);
        }
    },

    renderNutrition() {
        const container = document.getElementById('nutritionTemplatesList');
        if (!this.nutritionList.length) {
            container.innerHTML = '<p class="muted">Još nemaš predložaka prehrane. Klikni "+ Novi predložak prehrane".</p>';
            return;
        }
        container.innerHTML = this.nutritionList.map(t => `
            <div class="list-item" data-id="${t.id}">
                <div>
                    <h4>${this.escape(t.name)}</h4>
                    <div class="meta">
                        ${t.dayCount} ${t.dayCount === 1 ? 'dan' : 'dana'}
                        ${t.notes ? ' · ' + this.escape(t.notes.substring(0, 80)) : ''}
                    </div>
                </div>
                <span class="planned-actions">
                    <button class="btn-delete-icon delete-nut-tpl-btn" data-id="${t.id}" data-name="${this.escape(t.name)}" title="Obriši">🗑</button>
                    <span class="muted">→</span>
                </span>
            </div>
        `).join('');
        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', e => {
                if (e.target.closest('.delete-nut-tpl-btn')) return;
                App.switchTrainerView('nutrition');
                Nutrition.showDetail(parseInt(el.dataset.id));
            });
        });
        container.querySelectorAll('.delete-nut-tpl-btn').forEach(btn => {
            btn.addEventListener('click', async e => {
                e.stopPropagation();
                const id = parseInt(btn.dataset.id);
                const name = btn.dataset.name;
                if (!confirm(`Obrisati predložak "${name}"?`)) return;
                try {
                    await API.delete(`/nutrition-plans/${id}`);
                    await this.load();
                } catch (err) { alert(err.message); }
            });
        });
    },

    render() {
        const container = document.getElementById('templatesList');
        if (!container) return;
        if (!this.list.length) {
            container.innerHTML = '<p class="muted">Još nemaš predložaka. Klikni "+ Novi predložak" da napraviš kostur (dani + vježbe) koji ćeš kasnije ponovno koristiti.</p>';
            return;
        }
        container.innerHTML = this.list.map(t => `
            <div class="list-item" data-id="${t.id}">
                <div>
                    <h4>${this.escape(t.name)}</h4>
                    <div class="meta">
                        ${t.dayCount} ${t.dayCount === 1 ? 'dan' : 'dana'}
                        ${t.trainerExpectations ? ' · ' + this.escape(t.trainerExpectations.substring(0, 80)) : ''}
                    </div>
                </div>
                <span class="planned-actions">
                    <button class="btn-delete-icon delete-tpl-btn" data-id="${t.id}" data-name="${this.escape(t.name)}" title="Obriši predložak">🗑</button>
                    <span class="muted">→</span>
                </span>
            </div>
        `).join('');
        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', e => {
                if (e.target.closest('.delete-tpl-btn')) return;
                this.showDetail(parseInt(el.dataset.id));
            });
        });
        container.querySelectorAll('.delete-tpl-btn').forEach(btn => {
            btn.addEventListener('click', async e => {
                e.stopPropagation();
                const id = parseInt(btn.dataset.id);
                const name = btn.dataset.name;
                if (!confirm(`Obrisati predložak "${name}"? Postojeći klijenti planovi nastali iz njega ostaju netaknuti.`)) return;
                try {
                    await API.delete(`/training-plans/${id}`);
                    await this.load();
                } catch (err) {
                    alert(err.message);
                }
            });
        });
    },

    showNewForm() {
        document.getElementById('templateName').value = '';
        document.getElementById('templateExpectations').value = '';
        document.getElementById('newTemplateError').textContent = '';
        document.getElementById('newTemplateForm').classList.remove('hidden');
    },

    resetNewForm() {
        document.getElementById('newTemplateForm').classList.add('hidden');
    },

    async create() {
        const errorEl = document.getElementById('newTemplateError');
        errorEl.textContent = '';
        const name = document.getElementById('templateName').value.trim();
        const exp = document.getElementById('templateExpectations').value.trim();
        if (!name) { errorEl.textContent = 'Naziv je obavezan.'; return; }

        try {
            const created = await API.post('/training-plans/templates', {
                name,
                trainerExpectations: exp || null
            });
            this.resetNewForm();
            await this.load();
            await this.showDetail(created.id);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    async showDetail(id) {
        try {
            if (!Exercises.list.length) await Exercises.load();
            this.current = await API.get(`/training-plans/${id}`);
            document.getElementById('templatesList').classList.add('hidden');
            document.getElementById('newTemplateBtn').classList.add('hidden');
            document.getElementById('newTemplateForm').classList.add('hidden');
            document.getElementById('templateDetail').classList.remove('hidden');

            document.getElementById('templateDetailName').textContent = this.current.name;
            document.getElementById('templateDetailMeta').textContent =
                `Predložak · ${this.current.days.length} ${this.current.days.length === 1 ? 'dan' : 'dana'}`;
            document.getElementById('templateDetailExpectations').textContent =
                this.current.trainerExpectations ? `Opis: ${this.current.trainerExpectations}` : '';

            this.renderDays();
        } catch (err) {
            alert(err.message);
        }
    },

    showList() {
        document.getElementById('templateDetail').classList.add('hidden');
        document.getElementById('templatesList').classList.remove('hidden');
        document.getElementById('newTemplateBtn').classList.remove('hidden');
        this.current = null;
        this.load();
    },

    renderDays() {
        const container = document.getElementById('templateDaysList');
        if (!this.current.days.length) {
            container.innerHTML = '<p class="muted">Bez dana. Dodaj prvi dan iznad.</p>';
            return;
        }

        container.innerHTML = this.current.days.map(d => `
            <div class="exercise-block" data-day-id="${d.id}">
                <div class="row" style="justify-content: space-between; align-items: center;">
                    <h4>${this.dayNames[d.dayOfWeek]} — ${this.escape(d.label)}</h4>
                    <button class="icon-btn delete-day-btn" data-day-id="${d.id}" title="Obriši dan">×</button>
                </div>
                ${d.exercises.length === 0
                    ? '<p class="muted small">Bez vježbi.</p>'
                    : d.exercises.map((pe, idx) => `
                        <div class="planned-row">
                            <span class="planned-name"><strong>${this.escape(pe.exerciseName)}</strong></span>
                            <span class="planned-target">${Plans.planTargetLabel(pe)}</span>
                            ${pe.restSeconds ? `<span class="planned-rest">${pe.restSeconds}s</span>` : ''}
                            <span class="planned-actions">
                                <button class="move-pe-btn" data-pe-id="${pe.id}" data-direction="up" ${idx === 0 ? 'disabled' : ''} title="Pomakni gore">▲</button>
                                <button class="move-pe-btn" data-pe-id="${pe.id}" data-direction="down" ${idx === d.exercises.length - 1 ? 'disabled' : ''} title="Pomakni dolje">▼</button>
                                <button class="btn-delete delete-pe-btn" data-pe-id="${pe.id}" title="Obriši vježbu">🗑</button>
                            </span>
                        </div>
                    `).join('')}
                <div class="add-set-form add-pe-form" data-mode="reps">
                    <select class="day-exercise-select"></select>
                    <input type="number" class="pe-sets" placeholder="serije" min="1" value="3" title="Broj serija">
                    <input type="number" class="pe-reps" placeholder="rep" min="1" value="10" title="Ponavljanja">
                    <input type="number" class="pe-duration hidden" placeholder="min" step="0.1" min="0.1" value="2" title="Trajanje (min, decimalno)">
                    <input type="number" class="pe-weight" placeholder="kg" step="0.5" min="0" value="0" title="Težina">
                    <input type="number" class="pe-rest" placeholder="odmor s" min="0" max="3600" title="Odmor između serija">
                    <div class="pe-mode-toggle">
                        <label><input type="radio" name="tplPeMode-${d.id}" value="reps" checked> Pon.</label>
                        <label><input type="radio" name="tplPeMode-${d.id}" value="time"> Vrij.</label>
                    </div>
                    <button class="add-pe-btn">+</button>
                </div>
            </div>
        `).join('');

        container.querySelectorAll('.day-exercise-select').forEach(sel => {
            if (!Exercises.list.length) {
                sel.innerHTML = '<option value="">— nema vježbi —</option>';
                return;
            }
            const grouped = Plans.groupExercisesByType(Exercises.list);
            sel.innerHTML = grouped.map(g => `
                <optgroup label="${ExerciseTypeLabels[g.type] || g.type}">
                    ${g.items.map(e =>
                        `<option value="${e.id}">${this.escape(e.name)}${e.muscleGroup ? ' — ' + this.escape(e.muscleGroup) : ''}</option>`
                    ).join('')}
                </optgroup>
            `).join('');
        });

        container.querySelectorAll('.delete-day-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteDay(parseInt(btn.dataset.dayId)));
        });
        container.querySelectorAll('.delete-pe-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deletePlannedExercise(parseInt(btn.dataset.peId)));
        });
        container.querySelectorAll('.move-pe-btn').forEach(btn => {
            btn.addEventListener('click', () => this.movePlannedExercise(parseInt(btn.dataset.peId), btn.dataset.direction));
        });
        container.querySelectorAll('.add-pe-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addPlannedExercise(e.target.closest('.exercise-block')));
        });
        container.querySelectorAll('.add-pe-form').forEach(form => {
            const repsInput = form.querySelector('.pe-reps');
            const durInput = form.querySelector('.pe-duration');
            form.querySelectorAll('input[type="radio"]').forEach(r => {
                r.addEventListener('change', () => {
                    const mode = form.querySelector('input[type="radio"]:checked').value;
                    form.dataset.mode = mode;
                    repsInput.classList.toggle('hidden', mode !== 'reps');
                    durInput.classList.toggle('hidden', mode !== 'time');
                });
            });
        });
    },

    async addDay() {
        if (!this.current) return;
        const dow = document.getElementById('newTemplateDayOfWeek').value;
        const label = document.getElementById('newTemplateDayLabel').value.trim();
        if (!label) { alert('Unesi oznaku dana (npr. Push Day).'); return; }
        try {
            await API.post(`/training-plans/${this.current.id}/days`, { dayOfWeek: dow, label });
            document.getElementById('newTemplateDayLabel').value = '';
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteDay(dayId) {
        if (!confirm('Obrisati ovaj dan i sve vježbe?')) return;
        try {
            await API.delete(`/training-plans/days/${dayId}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async addPlannedExercise(block) {
        const dayId = parseInt(block.dataset.dayId);
        const form = block.querySelector('.add-pe-form');
        const mode = form.dataset.mode || 'reps';
        const exerciseId = parseInt(form.querySelector('.day-exercise-select').value);
        const sets = parseInt(form.querySelector('.pe-sets').value);
        const weight = parseFloat(form.querySelector('.pe-weight').value);
        const restRaw = form.querySelector('.pe-rest').value;
        const rest = restRaw === '' ? null : parseInt(restRaw);

        if (!exerciseId) { alert('Odaberi vježbu.'); return; }
        if (!sets) { alert('Popuni broj serija.'); return; }

        const payload = {
            exerciseId,
            order: 0,
            targetSets: sets,
            targetReps: 0,
            targetWeightKg: isNaN(weight) ? 0 : weight,
            restSeconds: Number.isFinite(rest) ? rest : null
        };

        if (mode === 'time') {
            const minutes = parseFloat(form.querySelector('.pe-duration').value);
            if (!minutes || minutes <= 0) { alert('Unesi trajanje u minutama (npr. 2 ili 1.5).'); return; }
            payload.targetDurationSeconds = Math.round(minutes * 60);
        } else {
            const reps = parseInt(form.querySelector('.pe-reps').value);
            if (!reps) { alert('Unesi broj ponavljanja.'); return; }
            payload.targetReps = reps;
        }

        const day = this.current.days.find(d => d.id === dayId);
        payload.order = day ? day.exercises.length + 1 : 1;

        try {
            await API.post(`/training-plans/days/${dayId}/exercises`, payload);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deletePlannedExercise(peId) {
        if (!confirm('Obrisati vježbu iz predloška?')) return;
        try {
            await API.delete(`/training-plans/exercises/${peId}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async movePlannedExercise(peId, direction) {
        try {
            await API.put(`/training-plans/exercises/${peId}/move?direction=${direction}`);
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async editCurrent() {
        if (!this.current) return;
        const newName = prompt('Naziv predloška:', this.current.name);
        if (newName === null) return;
        const newExp = prompt('Opis / očekivanja (može biti prazno):', this.current.trainerExpectations || '');
        if (newExp === null) return;
        try {
            await API.put(`/training-plans/templates/${this.current.id}`, {
                name: newName.trim() || this.current.name,
                trainerExpectations: newExp.trim() || null
            });
            await this.showDetail(this.current.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteCurrent() {
        if (!this.current) return;
        if (!confirm(`Obrisati predložak "${this.current.name}"? Postojeći klijenti planovi ostaju netaknuti.`)) return;
        try {
            await API.delete(`/training-plans/${this.current.id}`);
            this.showList();
        } catch (err) {
            alert(err.message);
        }
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s || '';
        return div.innerHTML;
    }
};
