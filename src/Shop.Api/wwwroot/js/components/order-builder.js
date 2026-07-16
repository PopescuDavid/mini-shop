import { api } from '../api-client.js';
import { store } from '../store.js';
import { sharedStyles } from '../shared-styles.js';
import { money } from '../format.js';

class OrderBuilder extends HTMLElement {
    #error = null;

    connectedCallback() {
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.adoptedStyleSheets = [sharedStyles];
        this.unsubscribe = store.subscribe(() => this.render());
        this.render();
    }

    disconnectedCallback() {
        this.unsubscribe?.();
    }

    render() {
        const { cart } = store.state;
        const subtotal = cart.reduce((sum, line) => sum + line.price * line.quantity, 0);

        this.shadowRoot.innerHTML = `
            <div class="card">
                <h2>New order</h2>
                <div id="body"></div>
                <p class="error" hidden></p>
            </div>`;

        const body = this.shadowRoot.getElementById('body');
        if (cart.length === 0) {
            body.innerHTML = `<p class="muted">Add products from the catalogue to start an order.</p>`;
        } else {
            this.#cart(body, cart, subtotal);
        }

        if (this.#error) {
            const error = this.shadowRoot.querySelector('.error');
            error.textContent = this.#error;
            error.hidden = false;
        }

        this.shadowRoot.querySelectorAll('[data-remove]').forEach(button =>
            button.addEventListener('click', () => store.removeFromCart(button.dataset.remove)));
        this.shadowRoot.getElementById('create')?.addEventListener('click', () => this.#create());
    }

    #cart(body, cart, subtotal) {
        body.innerHTML = `
            <table><tbody></tbody></table>
            <div class="row spread"><span class="muted">Subtotal</span><strong id="subtotal"></strong></div>
            <label class="field">Coupon code
                <input id="coupon" placeholder="e.g. SAVE10">
            </label>
            <button id="create" class="primary">Create order</button>`;

        const tbody = body.querySelector('tbody');
        for (const line of cart) {
            const tr = document.createElement('tr');

            const name = document.createElement('td');
            name.textContent = line.name;

            const quantity = document.createElement('td');
            quantity.className = 'num muted';
            quantity.textContent = `×${line.quantity}`;

            const lineTotal = document.createElement('td');
            lineTotal.className = 'num';
            lineTotal.textContent = money(line.price * line.quantity);

            const action = document.createElement('td');
            action.className = 'num';
            const remove = document.createElement('button');
            remove.className = 'ghost danger';
            remove.dataset.remove = line.productId;
            remove.textContent = '✕';
            action.append(remove);

            tr.append(name, quantity, lineTotal, action);
            tbody.append(tr);
        }

        body.querySelector('#subtotal').textContent = money(subtotal);
    }

    async #create() {
        this.#error = null;
        const items = store.state.cart.map(line => ({ productId: line.productId, quantity: line.quantity }));
        const couponCode = this.shadowRoot.getElementById('coupon').value.trim() || null;
        try {
            const order = await api.createOrder({ items, couponCode });
            store.clearCart();
            store.setActiveOrder(order.id);
        } catch (err) {
            this.#error = err.message;
            this.render();
        }
    }
}

customElements.define('order-builder', OrderBuilder);
