class ApiClient {
    #token = null;

    setToken(token) {
        this.#token = token;
    }

    async #request(path, options = {}) {
        const headers = { 'Content-Type': 'application/json', ...(options.headers ?? {}) };
        if (this.#token) headers.Authorization = `Bearer ${this.#token}`;

        const response = await fetch(path, { ...options, headers });

        if (!response.ok) {
            throw new Error(await this.#errorMessage(response));
        }
        return response.status === 204 ? null : response.json();
    }

    async #errorMessage(response) {
        try {
            const body = await response.json();
            return body.detail || body.title || `Request failed (${response.status})`;
        } catch {
            return `Request failed (${response.status})`;
        }
    }

    login(email, password) {
        return this.#request('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
    }

    getProducts(page, pageSize, sortBy, sortDir) {
        const query = new URLSearchParams({ page, pageSize, sortBy, sortDir });
        return this.#request(`/products?${query}`);
    }

    createOrder(payload) {
        return this.#request('/orders', { method: 'POST', body: JSON.stringify(payload) });
    }

    getOrder(id) {
        return this.#request(`/orders/${id}`);
    }

    updateOrder(id, payload) {
        return this.#request(`/orders/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
    }

    deleteOrder(id) {
        return this.#request(`/orders/${id}`, { method: 'DELETE' });
    }
}

export const api = new ApiClient();
