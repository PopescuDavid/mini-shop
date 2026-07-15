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
                ${cart.length === 0 ? this.#empty() : this.#cart(cart, subtotal)}
                ${this.#error ? `<p class="error">${this.#error}</p>` : ''}
            </div>`;

        this.shadowRoot.querySelectorAll('[data-remove]').forEach(button =>
            button.addEventListener('click', () => store.removeFromCart(button.dataset.remove)));
        this.shadowRoot.getElementById('create')?.addEventListener('click', () => this.#create());
    }

    #empty() {
        return `<p class="muted">Add products from the catalogue to start an order.</p>`;
    }

    #cart(cart, subtotal) {
        return `
            <table>
                <tbody>
                    ${cart.map(line => `
                        <tr>
                            <td>${line.name}</td>
                            <td class="num muted">×${line.quantity}</td>
                            <td class="num">${money(line.price * line.quantity)}</td>
                            <td class="num"><button class="ghost danger" data-remove="${line.productId}">✕</button></td>
                        </tr>`).join('')}
                </tbody>
            </table>
            <div class="row spread"><span class="muted">Subtotal</span><strong>${money(subtotal)}</strong></div>
            <label class="field">Coupon code
                <input id="coupon" placeholder="e.g. SAVE10">
            </label>
            <button id="create" class="primary">Create order</button>`;
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
