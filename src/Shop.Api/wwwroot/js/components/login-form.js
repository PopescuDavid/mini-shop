import { store } from '../store.js';
import { api } from '../api-client.js';
import { sharedStyles } from '../shared-styles.js';

class LoginForm extends HTMLElement {
    connectedCallback() {
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.adoptedStyleSheets = [sharedStyles];
        this.shadowRoot.innerHTML = `
            <form class="card">
                <h2>Sign in</h2>
                <label class="field">Email
                    <input name="email" type="email" value="demo@shop.test" required>
                </label>
                <label class="field">Password
                    <input name="password" type="password" value="Passw0rd!" required>
                </label>
                <button class="primary" type="submit">Log in</button>
                <p class="error" hidden></p>
            </form>`;
        this.shadowRoot.querySelector('form').addEventListener('submit', event => this.#submit(event));
    }

    async #submit(event) {
        event.preventDefault();
        const form = event.target;
        const error = this.shadowRoot.querySelector('.error');
        error.hidden = true;
        try {
            const { token } = await api.login(form.email.value, form.password.value);
            api.setToken(token);
            store.setSession(token, form.email.value);
        } catch {
            error.textContent = 'Login failed. Check your credentials and try again.';
            error.hidden = false;
        }
    }
}

customElements.define('login-form', LoginForm);
