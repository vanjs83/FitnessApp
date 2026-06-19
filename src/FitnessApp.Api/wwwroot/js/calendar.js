// Trainer calendar: month grid of their booked sessions, plus create / status actions.
const Calendar = {
    ref: new Date(),          // any day in the currently shown month
    selected: null,           // 'YYYY-MM-DD' of the open day panel
    appointments: [],
    clients: [],
    wired: false,

    async load() {
        this.wire();
        await Promise.all([this.loadClients(), this.loadMonth()]);
        this.renderGrid();
        this.renderDay();
    },

    wire() {
        if (this.wired) return;
        this.wired = true;
        document.getElementById('calPrevBtn').addEventListener('click', () => this.shiftMonth(-1));
        document.getElementById('calNextBtn').addEventListener('click', () => this.shiftMonth(1));
        document.getElementById('calNewBtn').addEventListener('click', () => this.toggleForm(true));
        document.getElementById('calCancelBtn').addEventListener('click', () => this.toggleForm(false));
        document.getElementById('calSaveBtn').addEventListener('click', () => this.save());
    },

    async shiftMonth(delta) {
        this.ref = new Date(this.ref.getFullYear(), this.ref.getMonth() + delta, 1);
        this.selected = null;
        await this.loadMonth();
        this.renderGrid();
        this.renderDay();
    },

    monthRange() {
        const y = this.ref.getFullYear(), m = this.ref.getMonth();
        const from = new Date(y, m, 1);
        const to = new Date(y, m + 1, 1);
        return { from: this.isoLocal(from), to: this.isoLocal(to) };
    },

    async loadMonth() {
        const { from, to } = this.monthRange();
        try {
            this.appointments = await API.get(`/appointments?from=${from}&to=${to}`) || [];
        } catch (e) {
            this.appointments = [];
        }
    },

    async loadClients() {
        this.clients = [];
        let page = 1;
        // Clients are paged at 25; pull every page so the dropdown is complete.
        for (;;) {
            const res = await API.get(`/trainers/me/clients?page=${page}`);
            const items = res.items || [];
            this.clients.push(...items);
            if (items.length < 25) break;
            page++;
        }
        const sel = document.getElementById('calClient');
        sel.innerHTML = `<option value="">${I18n.t('calendar.selectClient', 'Odaberi klijenta')}</option>` +
            this.clients.map(c => `<option value="${c.id}">${this.esc(c.fullName || c.email)}</option>`).join('');
    },

    renderGrid() {
        const y = this.ref.getFullYear(), m = this.ref.getMonth();
        document.getElementById('calMonthLabel').textContent =
            this.ref.toLocaleDateString(I18n.lang, { month: 'long', year: 'numeric' });

        // Count appointments per day key.
        const counts = {};
        for (const a of this.appointments) {
            const key = a.startsAt.slice(0, 10);
            counts[key] = (counts[key] || 0) + 1;
        }

        const weekdays = ['day.monday', 'day.tuesday', 'day.wednesday', 'day.thursday', 'day.friday', 'day.saturday', 'day.sunday'];
        let html = weekdays.map(k => `<div class="cal-wd">${I18n.t(k).slice(0, 3)}</div>`).join('');

        const first = new Date(y, m, 1);
        const lead = (first.getDay() + 6) % 7; // Monday-first offset
        const daysInMonth = new Date(y, m + 1, 0).getDate();
        const todayKey = this.isoLocal(new Date()).slice(0, 10);

        for (let i = 0; i < lead; i++) html += `<div class="cal-cell empty"></div>`;
        for (let d = 1; d <= daysInMonth; d++) {
            const key = `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
            const n = counts[key] || 0;
            const cls = ['cal-cell'];
            if (key === todayKey) cls.push('today');
            if (key === this.selected) cls.push('selected');
            html += `<div class="${cls.join(' ')}" data-day="${key}">
                <span class="cal-num">${d}</span>
                ${n ? `<span class="cal-badge">${n}</span>` : ''}
            </div>`;
        }

        const grid = document.getElementById('calGrid');
        grid.innerHTML = html;
        grid.querySelectorAll('.cal-cell[data-day]').forEach(cell => {
            cell.addEventListener('click', () => {
                this.selected = cell.dataset.day;
                this.renderGrid();
                this.renderDay();
            });
        });
    },

    renderDay() {
        const panel = document.getElementById('calDayPanel');
        if (!this.selected) { panel.classList.add('hidden'); return; }
        panel.classList.remove('hidden');

        const dayAppts = this.appointments
            .filter(a => a.startsAt.slice(0, 10) === this.selected)
            .sort((a, b) => a.startsAt.localeCompare(b.startsAt));

        document.getElementById('calDayTitle').textContent =
            new Date(this.selected + 'T00:00').toLocaleDateString(I18n.lang, { weekday: 'long', day: 'numeric', month: 'long' });

        const list = document.getElementById('calDayList');
        if (!dayAppts.length) {
            list.innerHTML = `<p class="muted small">${I18n.t('calendar.noSessions', 'Nema termina.')}</p>`;
            return;
        }

        list.innerHTML = dayAppts.map(a => {
            const time = a.startsAt.slice(11, 16);
            const typeLabel = I18n.t('calendar.' + (a.type === 'Online' ? 'online' : 'inPerson'));
            return `<div class="card-inset cal-item">
                <div class="row" style="justify-content:space-between;align-items:center">
                    <div>
                        <strong>${time}</strong> · ${this.esc(a.counterpartName)}
                        <span class="cal-status cal-${a.status}">${I18n.t('calendar.status.' + a.status, a.status)}</span>
                    </div>
                    <span class="muted small">${typeLabel}${a.location ? ' · ' + this.esc(a.location) : ''}</span>
                </div>
                ${a.notes ? `<p class="muted small">${this.esc(a.notes)}</p>` : ''}
                <div class="row">${this.actions(a)}</div>
            </div>`;
        }).join('');

        list.querySelectorAll('button[data-act]').forEach(btn => {
            btn.addEventListener('click', () => this.act(btn.dataset.act, Number(btn.dataset.id)));
        });
    },

    actions(a) {
        const btn = (act, key, cls = 'secondary') =>
            `<button class="${cls}" data-act="${act}" data-id="${a.id}">${I18n.t('calendar.' + key)}</button>`;
        if (a.status === 'Requested') return btn('confirm', 'confirm', '') + btn('cancel', 'cancel');
        if (a.status === 'Scheduled') return btn('complete', 'complete', '') + btn('no-show', 'noShow') + btn('cancel', 'cancel');
        return '';
    },

    async act(action, id) {
        await API.post(`/appointments/${id}/${action}`);
        await this.loadMonth();
        this.renderGrid();
        this.renderDay();
    },

    toggleForm(show) {
        document.getElementById('calNewForm').classList.toggle('hidden', !show);
        document.getElementById('calError').textContent = '';
        if (show && this.selected) {
            document.getElementById('calStart').value = this.selected + 'T09:00';
        }
    },

    async save() {
        const err = document.getElementById('calError');
        err.textContent = '';
        const clientId = document.getElementById('calClient').value;
        const startsAt = document.getElementById('calStart').value;
        if (!clientId || !startsAt) {
            err.textContent = I18n.t('calendar.required', 'Odaberi klijenta i vrijeme.');
            return;
        }
        try {
            await API.post('/appointments', {
                clientId,
                startsAt,
                durationMinutes: Number(document.getElementById('calDuration').value) || 60,
                type: document.getElementById('calType').value,
                location: document.getElementById('calLocation').value || null,
                notes: document.getElementById('calNotes').value || null
            });
            this.toggleForm(false);
            ['calLocation', 'calNotes'].forEach(id => document.getElementById(id).value = '');
            await this.loadMonth();
            this.renderGrid();
            this.renderDay();
        } catch (e) {
            err.textContent = (e && e.message) || I18n.t('common.error', 'Greška.');
        }
    },

    // Local-time ISO (no trailing Z) so the value matches what the trainer picked.
    isoLocal(d) {
        const p = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:00`;
    },

    esc(s) {
        return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
};
