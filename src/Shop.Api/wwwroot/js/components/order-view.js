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
                    <input id="orderId" placeholder="Order id">
                    <button id="load">Load</button>
                </div>
                <p class="error" hidden></p>
                <div id="body"></div>
            </div>`;

        this.shadowRoot.getElementById('orderId').value = this.#trackedId ?? '';

        if (this.#error) {
            const error = this.shadowRoot.querySelector('.error');
            error.textContent = this.#error;
            error.hidden = false;
        }

        const body = this.shadowRoot.getElementById('body');
        if (this.#order) {
            this.#details(body, this.#order);
        } else {
            body.innerHTML = `<p class="muted">Create an order or paste an id to view it.</p>`;
        }

        this.shadowRoot.getElementById('load').addEventListener('click', () => {
            const id = this.shadowRoot.getElementById('orderId').value.trim();
            if (id) { this.#trackedId = id; this.#load(id); }
        });
        this.shadowRoot.getElementById('delete')?.addEventListener('click', () => this.#delete(this.#order.id));
        this.shadowRoot.getElementById('place')?.addEventListener('click', () => this.#place(this.#order));
    }

    #details(body, order) {
        body.innerHTML = `
            <div class="row spread"><span class="muted">Status</span><span class="badge"></span></div>
            <table>
                <thead>
                    <tr><th>Item</th><th class="num">Qty</th><th class="num">Unit</th><th class="num">Line</th></tr>
                </thead>
                <tbody></tbody>
            </table>
            <div class="row spread"><span class="muted">Subtotal</span><span id="subtotal"></span></div>
            <div class="row spread discount" hidden><span class="muted"></span><span></span></div>
            <div class="row spread total"><strong>Total</strong><strong id="total"></strong></div>
            <div class="row actions">
                ${order.status === 'Draft' ? `<button id="place" class="primary">Place order</button>` : ''}
                <button id="delete" class="danger">Delete order</button>
            </div>`;

        body.querySelector('.badge').textContent = order.status;

        const tbody = body.querySelector('tbody');
        for (const item of order.items) {
            const tr = document.createElement('tr');

            const name = document.createElement('td');
            name.textContent = item.name;

            const quantity = document.createElement('td');
            quantity.className = 'num';
            quantity.textContent = item.quantity;

            const unit = document.createElement('td');
            unit.className = 'num';
            unit.textContent = money(item.unitPrice);

            const lineTotal = document.createElement('td');
            lineTotal.className = 'num';
            lineTotal.textContent = money(item.lineTotal);

            tr.append(name, quantity, unit, lineTotal);
            tbody.append(tr);
        }

        body.querySelector('#subtotal').textContent = money(order.subtotal);

        if (order.couponCode) {
            const discount = body.querySelector('.discount');
            discount.hidden = false;
            const [label, value] = discount.querySelectorAll('span');
            label.textContent = `Discount (${order.couponCode})`;
            value.textContent = `−${money(order.discount)}`;
        }

        body.querySelector('#total').textContent = money(order.total);
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

    async #place(order) {
        this.#error = null;
        try {
            this.#order = await api.updateOrder(order.id, {
                items: order.items.map(item => ({ productId: item.productId, quantity: item.quantity })),
                couponCode: order.couponCode,
                status: 'Placed'
            });
        } catch (err) {
            this.#error = err.message;
        }
        this.render();
    }
}

customElements.define('order-view', OrderView);
