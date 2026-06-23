// Month-grid calendar of booked sessions. Used by both roles:
//  - 'trainer': books sessions directly and confirms / completes / no-shows / cancels them.
//  - 'client':  proposes sessions (their trainer confirms) and can cancel/decline their own.
// The two modes render into separate DOM blocks (trainer 'cal*' ids, client 'cc*' ids);
// `mode` selects the id-set so the rest of the logic is shared.
const Calendar = {
    ids: {
        trainer: {
            grid: 'calGrid', monthLabel: 'calMonthLabel', prev: 'calPrevBtn', next: 'calNextBtn',
            dayPanel: 'calDayPanel', dayTitle: 'calDayTitle', dayList: 'calDayList',
            newBtn: 'calNewBtn', newForm: 'calNewForm', cancelBtn: 'calCancelBtn', saveBtn: 'calSaveBtn',
            client: 'calClient', start: 'calStart', duration: 'calDuration', type: 'calType',
            location: 'calLocation', notes: 'calNotes', error: 'calError',
            targetMode: 'calTargetMode', group: 'calGroup'
        },
        client: {
            grid: 'ccGrid', monthLabel: 'ccMonthLabel', prev: 'ccPrevBtn', next: 'ccNextBtn',
            dayPanel: 'ccDayPanel', dayTitle: 'ccDayTitle', dayList: 'ccDayList',
            newBtn: 'ccNewBtn', newForm: 'ccNewForm', cancelBtn: 'ccCancelBtn', saveBtn: 'ccSaveBtn',
            start: 'ccStart', duration: 'ccDuration', type: 'ccType',
            location: 'ccLocation', notes: 'ccNotes', error: 'ccError'
        }
    },

    mode: 'trainer',
    wiredMode: null,
    ref: new Date(),          // any day in the currently shown month
    selected: null,           // 'YYYY-MM-DD' of the open day panel
    appointments: [],
    clients: [],

    // Element by logical key for the current mode.
    el(key) {
        const id = this.ids[this.mode][key];
        return id ? document.getElementById(id) : null;
    },

    async load(mode = 'trainer') {
        this.mode = mode;
        this.wire();
        const tasks = [this.loadMonth()];
        if (mode === 'trainer') tasks.push(this.loadClients(), this.loadGroups());
        await Promise.all(tasks);
        this.renderGrid();
        this.renderDay();
    },

    // Both roles have a "new session" form: the trainer books it (Scheduled), the client
    // proposes it (Requested → trainer confirms), so the wiring is the same.
    wire() {
        if (this.wiredMode === this.mode) return;
        this.wiredMode = this.mode;
        this.el('prev').addEventListener('click', () => this.shiftMonth(-1));
        this.el('next').addEventListener('click', () => this.shiftMonth(1));
        this.el('newBtn').addEventListener('click', () => this.toggleForm(true));
        this.el('cancelBtn').addEventListener('click', () => this.toggleForm(false));
        this.el('saveBtn').addEventListener('click', () => this.save());
        // Trainer only: switching individual ⇄ group swaps the client/group picker.
        const target = this.el('targetMode');
        if (target) target.addEventListener('change', () => this.applyTarget());
    },

    // Shows the client picker for an individual session, the group picker for a group session.
    applyTarget() {
        const target = this.el('targetMode');
        if (!target) return;
        const isGroup = target.value === 'group';
        this.el('client').classList.toggle('hidden', isGroup);
        this.el('group').classList.toggle('hidden', !isGroup);
    },

    isGroupTarget() {
        const target = this.el('targetMode');
        return this.mode === 'trainer' && target && target.value === 'group';
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

    // One feed for the month: /appointments returns both individual and group sessions (group ones
    // carry isGroup + groupName + memberCount), so the grid and day panel render them uniformly.
    async loadMonth() {
        const { from, to } = this.monthRange();
        try {
            // Cache-buster: GET /appointments is client-cached 5 min, so a freshly booked session
            // wouldn't show on reload without a unique URL.
            this.appointments = await API.get(`/appointments?from=${from}&to=${to}&_=${Date.now()}`) || [];
        } catch {
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
        const sel = this.el('client');
        sel.innerHTML = `<option value="">${I18n.t('calendar.selectClient', 'Odaberi klijenta')}</option>` +
            this.clients.map(c => `<option value="${c.id}">${this.esc(c.fullName || c.email)}</option>`).join('');
    },

    // Populates the group picker (trainer only) for booking a group session from the calendar.
    async loadGroups() {
        let groups = [];
        try { groups = await API.get('/groups') || []; } catch { groups = []; }
        const sel = this.el('group');
        if (!sel) return;
        sel.innerHTML = `<option value="">${I18n.t('calendar.selectGroup', 'Odaberi grupu')}</option>` +
            groups.map(g => `<option value="${g.id}">${this.esc(g.name)} (${g.memberCount})</option>`).join('');
    },

    renderGrid() {
        const y = this.ref.getFullYear(), m = this.ref.getMonth();
        this.el('monthLabel').textContent =
            this.ref.toLocaleDateString(I18n.lang, { month: 'long', year: 'numeric' });

        // Group appointments per day so each cell can preview them as colored status dots.
        const byDay = {};
        for (const a of this.appointments) {
            const key = a.startsAt.slice(0, 10);
            (byDay[key] = byDay[key] || []).push(a);
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
            const appts = (byDay[key] || []).sort((a, b) => a.startsAt.localeCompare(b.startsAt));
            const n = appts.length;
            const cls = ['cal-cell'];
            if (n) cls.push('has-appts');
            if (key === todayKey) cls.push('today');
            if (key === this.selected) cls.push('selected');
            // Preview the day's sessions as status-colored dots (up to 3, then "+N").
            const dots = appts.slice(0, 3).map(a =>
                `<span class="cal-dot cal-${a.status}" title="${a.startsAt.slice(11, 16)} · ${this.esc(a.counterpartName)}"></span>`).join('');
            const more = n > 3 ? `<span class="cal-more">+${n - 3}</span>` : '';
            html += `<div class="${cls.join(' ')}" data-day="${key}">
                <span class="cal-num">${d}</span>
                ${n ? `<div class="cal-dots">${dots}${more}</div>` : ''}
            </div>`;
        }

        const grid = this.el('grid');
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
        const panel = this.el('dayPanel');
        if (!this.selected) { panel.classList.add('hidden'); return; }
        panel.classList.remove('hidden');

        const dayAppts = this.appointments
            .filter(a => a.startsAt.slice(0, 10) === this.selected)
            .sort((a, b) => a.startsAt.localeCompare(b.startsAt));

        this.el('dayTitle').textContent =
            new Date(this.selected + 'T00:00').toLocaleDateString(I18n.lang, { weekday: 'long', day: 'numeric', month: 'long' });

        const list = this.el('dayList');
        if (!dayAppts.length) {
            list.innerHTML = `<p class="muted small">${I18n.t('calendar.noSessions', 'Nema termina.')}</p>`;
            return;
        }

        list.innerHTML = dayAppts.map(a => {
            const time = a.startsAt.slice(11, 16);
            const typeLabel = I18n.t('calendar.' + (a.type === 'Online' ? 'online' : 'inPerson'));
            return `<div class="card-inset cal-item cal-edge-${a.status}">
                <div class="row" style="justify-content:space-between;align-items:center">
                    <div>
                        <strong>${time}</strong> · ${a.isGroup ? '👥 ' : ''}${this.esc(a.counterpartName)}${a.isGroup ? ` (${a.memberCount})` : ''}
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
        // Group session: only the owning trainer can cancel it, straight from the calendar.
        if (a.isGroup)
            return (this.mode === 'trainer' && a.status === 'Scheduled') ? btn('cancel', 'cancel') : '';
        // Clients can only cancel their own pending/booked sessions; the rest is trainer-driven.
        if (this.mode === 'client')
            return (a.status === 'Requested' || a.status === 'Scheduled') ? btn('cancel', 'cancel') : '';
        if (a.status === 'Requested') return btn('confirm', 'confirm', '') + btn('cancel', 'cancel');
        if (a.status === 'Scheduled') return btn('complete', 'complete', '') + btn('no-show', 'noShow') + btn('cancel', 'cancel');
        return '';
    },

    async act(action, id) {
        // Both individual and group sessions act through /appointments now.
        await API.post(`/appointments/${id}/${action}`);
        await this.loadMonth();
        this.renderGrid();
        this.renderDay();
    },

    toggleForm(show) {
        this.el('newForm').classList.toggle('hidden', !show);
        this.el('error').textContent = '';
        if (show) {
            // Block past times in the native picker (browser local "now").
            this.el('start').min = this.isoLocal(new Date()).slice(0, 16);
            if (this.selected) this.el('start').value = this.selected + 'T09:00';
            this.applyTarget();
        }
    },

    async save() {
        const err = this.el('error');
        err.textContent = '';
        const startsAt = this.el('start').value;
        const isGroup = this.isGroupTarget();
        const clientId = (this.mode === 'trainer' && !isGroup) ? this.el('client').value : null;
        const groupId = isGroup ? this.el('group').value : null;

        if (!startsAt) { err.textContent = I18n.t('calendar.requiredTime', 'Odaberi vrijeme.'); return; }
        if (this.mode === 'trainer' && !isGroup && !clientId) {
            err.textContent = I18n.t('calendar.required', 'Odaberi klijenta i vrijeme.'); return;
        }
        if (isGroup && !groupId) { err.textContent = I18n.t('calendar.selectGroup', 'Odaberi grupu'); return; }
        if (new Date(startsAt) < new Date()) {
            err.textContent = I18n.t('calendar.past', 'Vrijeme termina je u prošlosti.');
            return;
        }
        const body = {
            startsAt,
            durationMinutes: Number(this.el('duration').value) || 60,
            type: this.el('type').value,
            location: this.el('location').value || null,
            notes: this.el('notes').value || null
        };
        try {
            // All bookings go through /appointments: group (GroupId), trainer individual (ClientId),
            // or client proposal (/request).
            if (isGroup) await API.post('/appointments', { groupId: Number(groupId), ...body });
            else if (this.mode === 'trainer') await API.post('/appointments', { clientId, ...body });
            else await API.post('/appointments/request', body);
            this.toggleForm(false);
            ['location', 'notes'].forEach(k => this.el(k).value = '');
            await this.loadMonth();
            this.renderGrid();
            this.renderDay();
        } catch (e) {
            err.textContent = (e && e.message) || I18n.t('common.error', 'Greška.');
        }
    },

    // Local-time ISO (no trailing Z) so the value matches what the user picked.
    isoLocal(d) {
        const p = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:00`;
    },

    esc(s) {
        return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
};
