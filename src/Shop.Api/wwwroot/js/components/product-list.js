import { api } from '../api-client.js';
import { store } from '../store.js';
import { sharedStyles } from '../shared-styles.js';
import { money } from '../format.js';

class ProductList extends HTMLElement {
    #page = 1;
    #pageSize = 6;
    #sortBy = 'name';
    #sortDir = 'asc';
    #result = null;
    #error = null;

    connectedCallback() {
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.adoptedStyleSheets = [sharedStyles];
        this.#load();
    }

    async #load() {
        this.#error = null;
        try {
            this.#result = await api.getProducts(this.#page, this.#pageSize, this.#sortBy, this.#sortDir);
        } catch (err) {
            this.#error = err.message;
        }
        this.render();
    }

    render() {
        if (this.#error) {
            this.shadowRoot.innerHTML = `<div class="card"><p class="error">${this.#error}</p></div>`;
            return;
        }
        if (!this.#result) {
            this.shadowRoot.innerHTML = `<div class="card"><p class="muted">Loading products…</p></div>`;
            return;
        }

        const { items, page, pageSize, totalCount } = this.#result;
        const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

        this.shadowRoot.innerHTML = `
            <div class="card">
                <div class="row spread">
                    <h2>Products</h2>
                    <label class="muted sort">Sort
                        <select id="sort">
                            <option value="name:asc">Name ↑</option>
                            <option value="name:desc">Name ↓</option>
                            <option value="price:asc">Price ↑</option>
                            <option value="price:desc">Price ↓</option>
                            <option value="stock:desc">Stock ↓</option>
                        </select>
                    </label>
                </div>
                <table>
                    <thead>
                        <tr><th>Name</th><th>Category</th><th class="num">Price</th><th class="num">Stock</th><th></th></tr>
                    </thead>
                    <tbody>
                        ${items.map(product => this.#row(product)).join('')}
                    </tbody>
                </table>
                <div class="row spread pager">
                    <button id="prev" ${page <= 1 ? 'disabled' : ''}>Prev</button>
                    <span class="muted">Page ${page} of ${totalPages}</span>
                    <button id="next" ${page >= totalPages ? 'disabled' : ''}>Next</button>
                </div>
            </div>`;

        this.#bind(items, totalPages);
    }

    #row(product) {
        return `
            <tr>
                <td>${product.name}</td>
                <td class="muted">${product.category}</td>
                <td class="num">${money(product.price)}</td>
                <td class="num">${product.stockQuantity}</td>
                <td class="num">
                    <input class="qty" type="number" min="1" value="1" data-qty="${product.id}">
                    <button data-add="${product.id}">Add</button>
                </td>
            </tr>`;
    }

    #bind(items, totalPages) {
        const sort = this.shadowRoot.getElementById('sort');
        sort.value = `${this.#sortBy}:${this.#sortDir}`;
        sort.addEventListener('change', () => {
            [this.#sortBy, this.#sortDir] = sort.value.split(':');
            this.#page = 1;
            this.#load();
        });

        this.shadowRoot.getElementById('prev').addEventListener('click', () => {
            if (this.#page > 1) { this.#page -= 1; this.#load(); }
        });
        this.shadowRoot.getElementById('next').addEventListener('click', () => {
            if (this.#page < totalPages) { this.#page += 1; this.#load(); }
        });

        this.shadowRoot.querySelectorAll('[data-add]').forEach(button => {
            button.addEventListener('click', () => {
                const product = items.find(p => p.id === button.dataset.add);
                const input = this.shadowRoot.querySelector(`[data-qty="${button.dataset.add}"]`);
                const quantity = Math.max(1, Number.parseInt(input.value, 10) || 1);
                store.addToCart(product, quantity);
            });
        });
    }
}

customElements.define('product-list', ProductList);
