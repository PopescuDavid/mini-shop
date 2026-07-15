import { api } from '../api-client.js';
import { store } from '../store.js';
import { sharedStyles } from '../shared-styles.js';
import { money } from '../format.js';

class OrderView extends HTMLElement {
    #order = null;
    #error = null;
    #trackedId = null;

    connectedCallback() {
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.adoptedStyleSheets = [sharedStyles];
        this.unsubscribe = store.subscribe(() => this.#syncFromStore());
        this.render();
    }

    disconnectedCallback() {
        this.unsubscribe?.();
    }

    #syncFromStore() {
        const id = store.state.activeOrderId;
        if (id && id !== this.#trackedId) {
            this.#trackedId = id;
            this.#load(id);
        }
    }

    async #load(id) {
        this.#error = null;
        try {
            this.#order = await api.getOrder(id);
        } catch (err) {
            this.#error = err.message;
            this.#order = null;
        }
        this.render();
    }

    render() {
        this.shadowRoot.innerHTML = `
            <div class="card">
                <h2>Order</h2>
                <div class="row">
                    <input id="orderId" placeholder="Order id" value="${this.#trackedId ?? ''}">
                    <button id="load">Load</button>
                </div>
                ${this.#error ? `<p class="error">${this.#error}</p>` : ''}
                ${this.#order ? this.#details(this.#order) : `<p class="muted">Create an order or paste an id to view it.</p>`}
            </div>`;

        this.shadowRoot.getElementById('load').addEventListener('click', () => {
            const id = this.shadowRoot.getElementById('orderId').value.trim();
            if (id) { this.#trackedId = id; this.#load(id); }
        });
        this.shadowRoot.getElementById('delete')?.addEventListener('click', () => this.#delete(this.#order.id));
    }

    #details(order) {
        return `
            <div class="row spread"><span class="muted">Status</span><span class="badge">${order.status}</span></div>
            <table>
                <thead>
                    <tr><th>Item</th><th class="num">Qty</th><th class="num">Unit</th><th class="num">Line</th></tr>
                </thead>
                <tbody>
                    ${order.items.map(item => `
                        <tr>
                            <td>${item.name}</td>
                            <td class="num">${item.quantity}</td>
                            <td class="num">${money(item.unitPrice)}</td>
                            <td class="num">${money(item.lineTotal)}</td>
                        </tr>`).join('')}
                </tbody>
            </table>
            <div class="row spread"><span class="muted">Subtotal</span><span>${money(order.subtotal)}</span></div>
            ${order.couponCode ? `<div class="row spread"><span class="muted">Discount (${order.couponCode})</span><span>−${money(order.discount)}</span></div>` : ''}
            <div class="row spread total"><strong>Total</strong><strong>${money(order.total)}</strong></div>
            <button id="delete" class="danger" style="margin-top:0.75rem">Delete order</button>`;
    }

    async #delete(id) {
        this.#error = null;
        try {
            await api.deleteOrder(id);
            this.#order = null;
            this.#trackedId = null;
            if (store.state.activeOrderId === id) store.setActiveOrder(null);
            this.render();
        } catch (err) {
            this.#error = err.message;
            this.render();
        }
    }
}

customElements.define('order-view', OrderView);
