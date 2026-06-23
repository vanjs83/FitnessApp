// Trainer-only group management: create groups, manage members (search-to-add) and delete.
// Group sessions are booked from the calendar (group target) and shown there.
const Groups = {
    groups: [],
    clients: [],
    wired: false,

    async load() {
        this.wire();
        await Promise.all([this.loadClients(), this.loadGroups()]);
        this.render();
    },

    wire() {
        if (this.wired) return;
        this.wired = true;
        document.getElementById('groupNewBtn').addEventListener('click', () => this.toggleCreate(true));
        document.getElementById('groupCancelBtn').addEventListener('click', () => this.toggleCreate(false));
        document.getElementById('groupSaveBtn').addEventListener('click', () => this.create());

        const list = document.getElementById('groupsList');
        // Click actions on the dynamically rendered group cards.
        list.addEventListener('click', e => {
            const btn = e.target.closest('button[data-action]');
            if (!btn) return;
            const card = btn.closest('.group-card');
            const groupId = Number(card.dataset.groupId);
            const action = btn.dataset.action;
            if (action === 'delete') this.del(groupId);
            else if (action === 'remove-member') this.removeMember(groupId, btn.dataset.clientId);
            else if (action === 'add-member') this.addMember(groupId, btn.dataset.clientId);
        });
        // Live client search inside each card's "add member" box.
        list.addEventListener('input', e => {
            const search = e.target.closest('.group-add-search');
            if (!search) return;
            this.renderAddResults(search.closest('.group-card'), search.value);
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

    toggleCreate(show) {
        document.getElementById('groupCreateForm').classList.toggle('hidden', !show);
        document.getElementById('groupError').textContent = '';
        if (show) document.getElementById('groupName').value = '';
    },

    async create() {
        const err = document.getElementById('groupError');
        err.textContent = '';
        const name = document.getElementById('groupName').value.trim();
        if (!name) { err.textContent = I18n.t('groups.nameRequired', 'Unesi naziv grupe.'); return; }
        try {
            // Group starts empty; members are added via search on the card.
            await API.post('/groups', { name, clientIds: [] });
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

    // Filtered list of the trainer's clients not yet in the group, matching the search text.
    renderAddResults(card, query) {
        const groupId = Number(card.dataset.groupId);
        const group = this.groups.find(g => g.id === groupId);
        if (!group) return;
        const memberIds = new Set(group.members.map(m => m.clientId));
        const q = query.trim().toLowerCase();
        const box = card.querySelector('.group-add-results');
        if (!q) { box.innerHTML = ''; return; }

        const results = this.clients
            .filter(c => !memberIds.has(c.id))
            .filter(c => (c.fullName || '').toLowerCase().includes(q) || (c.email || '').toLowerCase().includes(q))
            .slice(0, 8);

        box.innerHTML = results.length
            ? results.map(c => `<button class="group-add-result" data-action="add-member" data-client-id="${c.id}">+ ${this.esc(c.fullName || c.email)}</button>`).join('')
            : `<span class="muted small">${I18n.t('groups.noMatches', 'Nema rezultata.')}</span>`;
    },

    render() {
        const list = document.getElementById('groupsList');
        if (!this.groups.length) {
            list.innerHTML = `<p class="muted small">${I18n.t('groups.empty', 'Nemaš grupa.')}</p>`;
            return;
        }

        list.innerHTML = this.groups.map(g => {
            const chips = g.members.length
                ? g.members.map(m => `<span class="group-chip">${this.esc(m.fullName)}
                       <button data-action="remove-member" data-client-id="${m.clientId}" title="${I18n.t('common.delete')}">&#10005;</button></span>`).join('')
                : `<span class="muted small">${I18n.t('groups.noMembers', 'Bez članova.')}</span>`;

            return `<div class="card group-card" data-group-id="${g.id}">
                <div class="group-head">
                    <strong>${this.esc(g.name)} <span class="muted small">(${g.memberCount})</span></strong>
                    <button class="secondary danger" data-action="delete" data-i18n="common.delete">Obriši</button>
                </div>
                <div class="group-chips">${chips}</div>
                <div class="group-add">
                    <input type="text" class="group-add-search" data-i18n-ph="groups.searchPh" placeholder="🔍 Pretraži klijente za dodavanje...">
                    <div class="group-add-results"></div>
                </div>
            </div>`;
        }).join('');

        if (window.I18n && I18n.apply) I18n.apply(list);
    },

    esc(s) {
        return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
};
