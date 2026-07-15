const sheet = new CSSStyleSheet();

sheet.replaceSync(`
    :host { display: block; }
    *, *::before, *::after { box-sizing: border-box; }

    h2 { margin: 0; font-size: 1.05rem; font-weight: 600; }

    .topbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 1rem 1.5rem;
        background: var(--surface);
        border-bottom: 1px solid var(--border);
    }
    .brand { font-weight: 700; font-size: 1.15rem; }
    .session { display: flex; align-items: center; gap: 0.75rem; }

    .center { max-width: 380px; margin: 4rem auto; padding: 0 1rem; }
    .layout {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 1.25rem;
        align-items: start;
        max-width: 1040px;
        margin: 1.5rem auto;
        padding: 0 1.5rem;
    }
    @media (max-width: 820px) { .layout { grid-template-columns: 1fr; } }

    .card {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: 12px;
        padding: 1.25rem;
        margin-bottom: 1.25rem;
    }

    .row { display: flex; align-items: center; gap: 0.5rem; }
    .row.spread { justify-content: space-between; }
    .row + .row, .card > table + .row, .row + label, .row + button { margin-top: 0.75rem; }

    .muted { color: var(--muted); font-size: 0.9rem; }
    .error { color: var(--danger); font-size: 0.9rem; margin: 0.75rem 0 0; }
    .num { text-align: right; white-space: nowrap; }

    table { width: 100%; border-collapse: collapse; margin: 0.75rem 0; font-size: 0.92rem; }
    th { text-align: left; font-weight: 600; color: var(--muted); font-size: 0.78rem; text-transform: uppercase; letter-spacing: 0.03em; }
    th.num { text-align: right; }
    td, th { padding: 0.5rem 0.4rem; border-bottom: 1px solid var(--border); }
    tbody tr:last-child td { border-bottom: none; }

    label.field { display: block; margin: 0.75rem 0; font-size: 0.9rem; color: var(--muted); }
    input, select {
        font: inherit;
        padding: 0.5rem 0.6rem;
        border: 1px solid var(--border);
        border-radius: 8px;
        background: #fff;
        color: var(--text);
        width: 100%;
    }
    label.field input { margin-top: 0.35rem; }
    input.qty { width: 4rem; display: inline-block; }

    button {
        font: inherit;
        cursor: pointer;
        padding: 0.5rem 0.9rem;
        border-radius: 8px;
        border: 1px solid var(--border);
        background: #fff;
        color: var(--text);
    }
    button:hover:not(:disabled) { border-color: var(--muted); }
    button:disabled { opacity: 0.5; cursor: default; }
    button.primary { background: var(--primary); border-color: var(--primary); color: var(--primary-contrast); width: 100%; margin-top: 0.75rem; }
    button.danger { color: var(--danger); border-color: var(--border); }
    button.ghost { border-color: transparent; background: transparent; padding: 0.35rem 0.5rem; }

    .badge { font-size: 0.8rem; padding: 0.15rem 0.6rem; border-radius: 999px; background: #eff6ff; color: var(--primary); border: 1px solid #dbeafe; }
    .total { border-top: 1px solid var(--border); padding-top: 0.6rem; margin-top: 0.3rem; }
    .pager { margin-top: 0.5rem; }
    .sort select { width: auto; }
    .actions { margin-top: 0.75rem; }
    .actions button { flex: 1; width: auto; margin-top: 0; }
`);

export const sharedStyles = sheet;
