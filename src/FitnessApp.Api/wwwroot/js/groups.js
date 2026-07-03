// Trainer-only group management: create groups, manage members (search-to-add) and delete.
// Group sessions are booked from the calendar (group target) and shown there.
const Groups = {
    groups: [],
    clients: [],
    wired: false,
    _addingKeys: new Set(),

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
            else if (action === 'message') this.openMessage(groupId);
        });

        // Group message modal.
        document.getElementById('closeGroupMsgBtn').addEventListener('click', () => this.closeMessage());
        document.getElementById('cancelGroupMsgBtn').addEventListener('click', () => this.closeMessage());
        document.getElementById('sendGroupMsgBtn').addEventListener('click', () => this.sendMessage());
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
            // Cache-buster: GET /groups is client-cached, so member/group edits show immediately.
            this.groups = await API.get(`/groups?_=${Date.now()}`) || [];
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
        // A slow connection can make the button feel unresponsive; guard against a double-click
        // re-sending the same add (which would hit the unique member index and 500).
        const key = `${groupId}:${clientId}`;
        if (this._addingKeys.has(key)) return;
        this._addingKeys.add(key);
        try {
            await API.post(`/groups/${groupId}/members`, { clientId });
            await this.loadGroups();
            this.render();
        } catch (e) {
            alert((e && e.message) || I18n.t('common.error', 'Greška.'));
        } finally {
            this._addingKeys.delete(key);
        }
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

    openMessage(groupId) {
        const group = this.groups.find(g => g.id === groupId);
        if (!group) return;
        this.msgGroupId = groupId;
        document.getElementById('groupMsgTo').textContent =
            `${group.name} · ${group.memberCount} ${I18n.t('groups.msg.members', 'članova')}`;
        document.getElementById('groupMsgSubject').value = '';
        document.getElementById('groupMsgBody').value = '';
        document.getElementById('groupMsgEmail').checked = true;
        document.getElementById('groupMsgPush').checked = true;
        document.getElementById('groupMsgSendAt').value = '';
        document.getElementById('groupMsgResult').textContent = '';
        document.getElementById('groupMessageModal').classList.remove('hidden');
    },

    closeMessage() {
        document.getElementById('groupMessageModal').classList.add('hidden');
        this.msgGroupId = null;
    },

    async sendMessage() {
        const result = document.getElementById('groupMsgResult');
        result.textContent = '';
        const subject = document.getElementById('groupMsgSubject').value.trim();
        const body = document.getElementById('groupMsgBody').value.trim();
        const email = document.getElementById('groupMsgEmail').checked;
        const push = document.getElementById('groupMsgPush').checked;

        if (!subject || !body) { result.textContent = I18n.t('groups.msg.required', 'Unesi predmet i poruku.'); return; }
        if (!email && !push) { result.textContent = I18n.t('groups.msg.channelRequired', 'Odaberi barem jedan kanal.'); return; }

        const btn = document.getElementById('sendGroupMsgBtn');
        btn.disabled = true;
        try {
            // Scheduled send: if a time is picked, queue it instead of sending now. A group can get
            // both channels; the server creates one scheduled row per channel from a single request.
            const localDt = document.getElementById('groupMsgSendAt').value;
            if (localDt) {
                const sendAt = new Date(localDt);
                if (sendAt.getTime() <= Date.now()) { result.textContent = 'Vrijeme slanja mora biti u budućnosti.'; return; }
                const sendAtUtc = sendAt.toISOString();
                const channels = [];
                if (push) channels.push('Push');
                if (email) channels.push('Email');
                await API.post('/scheduledmessages', {
                    channels, audience: 'Group', userId: null, groupId: this.msgGroupId, subject, body, sendAtUtc
                });
                alert(`Zakazano za ${sendAt.toLocaleString('hr-HR')}.`);
                this.closeMessage();
                return;
            }

            await API.post(`/groups/${this.msgGroupId}/message`, { subject, body, email, push });
            alert(I18n.t('groups.msg.sent', 'Poruka je u redu za slanje.'));
            this.closeMessage();
        } catch (e) {
            result.textContent = (e && e.message) || I18n.t('common.error', 'Greška.');
        } finally {
            btn.disabled = false;
        }
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
                    <span class="row">
                        <button class="secondary" data-action="message" data-i18n="groups.msg.btn">✉️ Poruka</button>
                        <button class="secondary danger" data-action="delete" data-i18n="common.delete">Obriši</button>
                    </span>
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
