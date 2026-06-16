// Shared pager control for server-paginated lists. Renders prev / "page X of Y" / next
// into `container` from a PagedResult-shaped `meta` ({ page, totalPages, totalCount }),
// and calls `onGo(newPage)` when the user navigates. Hidden when there is only one page.
const Pagination = {
    render(container, meta, onGo) {
        if (!container) return;
        const totalPages = meta ? meta.totalPages : 0;
        const page = meta ? meta.page : 1;

        if (!totalPages || totalPages <= 1) {
            container.innerHTML = '';
            return;
        }

        const label = (typeof I18n !== 'undefined')
            ? I18n.tf('common.pageOf', { page, total: totalPages })
            : `${page} / ${totalPages}`;

        container.innerHTML = `
            <button class="pager-btn" data-pg="prev" ${page <= 1 ? 'disabled' : ''} aria-label="prev">‹</button>
            <span class="pager-info">${label}</span>
            <button class="pager-btn" data-pg="next" ${page >= totalPages ? 'disabled' : ''} aria-label="next">›</button>
        `;

        const prev = container.querySelector('[data-pg="prev"]');
        const next = container.querySelector('[data-pg="next"]');
        if (prev) prev.addEventListener('click', () => onGo(page - 1));
        if (next) next.addEventListener('click', () => onGo(page + 1));
    }
};
