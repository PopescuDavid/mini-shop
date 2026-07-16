import { store } from '../store.js';
import { api } from '../api-client.js';
import { sharedStyles } from '../shared-styles.js';
import './login-form.js';
import './product-list.js';
import './order-builder.js';
import './order-view.js';

class ShopApp extends HTMLElement {
    #loggedIn = null;

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
        const loggedIn = Boolean(store.state.token);
        if (loggedIn === this.#loggedIn) return;
        this.#loggedIn = loggedIn;
        loggedIn ? this.#renderShop() : this.#renderLogin();
    }

    #renderLogin() {
        this.shadowRoot.innerHTML = `
            <header class="topbar"><span class="brand">Mini Shop</span></header>
            <main class="center"></main>`;
        this.shadowRoot.querySelector('main').append(document.createElement('login-form'));
    }

    #renderShop() {
        this.shadowRoot.innerHTML = `
            <header class="topbar">
                <span class="brand">Mini Shop</span>
                <span class="session">
                    <span class="muted" id="user"></span>
                    <button class="ghost" id="logout">Log out</button>
                </span>
            </header>
            <main class="layout">
                <section id="catalogue"></section>
                <aside id="sidebar"></aside>
            </main>`;

        this.shadowRoot.getElementById('user').textContent = store.state.user;

        this.shadowRoot.getElementById('logout').addEventListener('click', () => {
            api.setToken(null);
            store.clearSession();
        });
        this.shadowRoot.getElementById('catalogue').append(document.createElement('product-list'));
        const sidebar = this.shadowRoot.getElementById('sidebar');
        sidebar.append(document.createElement('order-builder'));
        sidebar.append(document.createElement('order-view'));
    }
}

customElements.define('shop-app', ShopApp);
