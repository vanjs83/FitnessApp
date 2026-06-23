// Trainer-only group management: create groups from their clients, add/remove members, book a
// group training session (pushes to every member) and delete groups.
const Groups = {
    groups: [],
    clients: [],
    wired: false,

    async load() {
        this.wire();
        await Promise.all([this.loadClients(), this.loadGroups()]);
        this.renderCreateChecklist();
        this.render();
    },

    wire() {
        if (this.wired) return;
        this.wired = true;
        document.getElementById('groupNewBtn').addEventListener('click', () => this.toggleCreate(true));
        document.getElementById('groupCancelBtn').addEventListener('click', () => this.toggleCreate(false));
        document.getElementById('groupSaveBtn').addEventListener('click', () => this.create());

        // Delegated handlers for the dynamically rendered group cards.
        const list = document.getElementById('groupsList');
        list.addEventListener('click', e => {
            const btn = e.target.closest('button[data-action]');
            if (!btn) return;
            const card = btn.closest('.group-card');
            const groupId = Number(card.dataset.groupId);
            const action = btn.dataset.action;
            if (action === 'delete') this.del(groupId);
            else if (action === 'remove-member') this.removeMember(groupId, btn.dataset.clientId);
            else if (action === 'toggle-book') card.querySelector('.group-book-form').classList.toggle('hidden');
            else if (action === 'book') this.book(groupId, card);
            else if (action === 'add-member') {
                const sel = card.querySelector('.group-add-select');
                if (sel.value) this.addMember(groupId, sel.value);
            }
        });
    },

    async loadClients() {
        this.clients = [];
        let page = 1;
        for (;;) {
            const res = await API.get(`/trainers/me/clients?page=${page}`);
            const items = res.items || [];
            this.clients.push(...items);
            if (items.length < 25) break;
            page++;
        }
    },

    async loadGroups() {
        try {
            this.groups = await API.get('/groups') || [];
        } catch {
            this.groups = [];
        }
    },

    clientName(id) {
        const c = this.clients.find(x => x.id === id);
        return c ? (c.fullName || c.email) : id;
    },

    toggleCreate(show) {
        document.getElementById('groupCreateForm').classList.toggle('hidden', !show);
        document.getElementById('groupError').textContent = '';
        if (show) document.getElementById('groupName').value = '';
        if (show) this.renderCreateChecklist();
    },

    // Checkboxes of the trainer's clients for the new-group form.
    renderCreateChecklist() {
        const box = document.getElementById('groupClientChecklist');
        box.innerHTML = this.clients.length
            ? this.clients.map(c => `<label class="group-check"><input type="checkbox" value="${c.id}"> ${this.esc(c.fullName || c.email)}</label>`).join('')
            : `<p class="muted small">${I18n.t('groups.noClients', 'Nemaš klijenata.')}</p>`;
    },

    async create() {
        const err = document.getElementById('groupError');
        err.textContent = '';
        const name = document.getElementById('groupName').value.trim();
        if (!name) { err.textContent = I18n.t('groups.nameRequired', 'Unesi naziv grupe.'); return; }
        const clientIds = [...document.querySelectorAll('#groupClientChecklist input:checked')].map(i => i.value);
        try {
            await API.post('/groups', { name, clientIds });
            this.toggleCreate(false);
            await this.loadGroups();
            this.render();
        } catch (e) {
            err.textContent = (e && e.message) || I18n.t('common.error', 'Greška.');
        }
    },

    async addMember(groupId, clientId) {
        try {
            await API.post(`/groups/${groupId}/members`, { clientId });
            await this.loadGroups();
            this.render();
        } catch (e) { alert((e && e.message) || I18n.t('common.error', 'Greška.')); }
    },

    async removeMember(groupId, clientId) {
        try {
            await API.delete(`/groups/${groupId}/members/${clientId}`);
            await this.loadGroups();
            this.render();
        } catch (e) { alert((e && e.message) || I18n.t('common.error', 'Greška.')); }
    },

    async del(groupId) {
        if (!confirm(I18n.t('groups.deleteConfirm', 'Obrisati grupu?'))) return;
        try {
            await API.delete(`/groups/${groupId}`);
            await this.loadGroups();
            this.render();
        } catch (e) { alert((e && e.message) || I18n.t('common.error', 'Greška.')); }
    },

    async book(groupId, card) {
        const err = card.querySelector('.group-book-error');
        err.textContent = '';
        const startsAt = card.querySelector('.group-book-start').value;
        if (!startsAt) { err.textContent = I18n.t('calendar.requiredTime', 'Odaberi vrijeme.'); return; }
        if (new Date(startsAt) < new Date()) { err.textContent = I18n.t('calendar.past', 'Vrijeme termina je u prošlosti.'); return; }
        try {
            await API.post(`/groups/${groupId}/sessions`, {
                startsAt,
                durationMinutes: Number(card.querySelector('.group-book-duration').value) || 60,
                type: card.querySelector('.group-book-type').value,
                location: card.querySelector('.group-book-location').value || null,
                notes: card.querySelector('.group-book-notes').value || null
            });
            card.querySelector('.group-book-form').classList.add('hidden');
            err.textContent = '';
            alert(I18n.t('groups.booked', 'Grupni termin zakazan.'));
        } catch (e) {
            err.textContent = (e && e.message) || I18n.t('common.error', 'Greška.');
        }
    },

    render() {
        const list = document.getElementById('groupsList');
        if (!this.groups.length) {
            list.innerHTML = `<p class="muted small">${I18n.t('groups.empty', 'Nemaš grupa.')}</p>`;
            return;
        }
        const minNow = new Date().toISOString().slice(0, 16);

        list.innerHTML = this.groups.map(g => {
            const chips = g.members.length
                ? g.members.map(m => `<span class="group-chip">${this.esc(m.fullName)}
                       <button data-action="remove-member" data-client-id="${m.clientId}" title="${I18n.t('common.delete')}">&#10005;</button></span>`).join('')
                : `<span class="muted small">${I18n.t('groups.noMembers', 'Bez članova.')}</span>`;

            // Clients not yet in the group → addable.
            const memberIds = new Set(g.members.map(m => m.clientId));
            const addable = this.clients.filter(c => !memberIds.has(c.id));
            const addSelect = addable.length
                ? `<select class="group-add-select"><option value="">${I18n.t('groups.addMember', '+ Dodaj člana')}</option>
                     ${addable.map(c => `<option value="${c.id}">${this.esc(c.fullName || c.email)}</option>`).join('')}</select>
                   <button class="secondary" data-action="add-member" data-i18n="groups.add">Dodaj</button>`
                : '';

            return `<div class="card group-card" data-group-id="${g.id}">
                <div class="row" style="justify-content:space-between;align-items:center">
                    <strong>${this.esc(g.name)} <span class="muted small">(${g.memberCount})</span></strong>
                    <div class="row">
                        <button data-action="toggle-book" data-i18n="groups.book">Zakaži termin</button>
                        <button class="secondary danger" data-action="delete" data-i18n="common.delete">Obriši</button>
                    </div>
                </div>
                <div class="group-chips">${chips}</div>
                <div class="row group-add">${addSelect}</div>

                <div class="group-book-form card-inset hidden">
                    <h4 data-i18n="groups.bookTitle">Grupni termin</h4>
                    <input type="datetime-local" class="group-book-start" min="${minNow}">
                    <input type="number" class="group-book-duration" min="5" max="480" value="60" data-i18n-ph="calendar.durationPh" placeholder="Trajanje (min)">
                    <select class="group-book-type">
                        <option value="InPerson" data-i18n="calendar.inPerson">Uživo</option>
                        <option value="Online" data-i18n="calendar.online">Online</option>
                    </select>
                    <input type="text" class="group-book-location" data-i18n-ph="calendar.locationPh" placeholder="Lokacija ili link">
                    <textarea class="group-book-notes" data-i18n-ph="calendar.notesPh" placeholder="Napomene..."></textarea>
                    <div class="row">
                        <button data-action="book" data-i18n="calendar.send">Pošalji</button>
                    </div>
                    <div class="error group-book-error"></div>
                </div>
            </div>`;
        }).join('');

        if (window.I18n && I18n.apply) I18n.apply(list);
    },

    esc(s) {
        return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
};
