class Store extends EventTarget {
    #state = { token: null, user: null, cart: [], activeOrderId: null };

    get state() {
        return this.#state;
    }

    #commit(patch) {
        this.#state = { ...this.#state, ...patch };
        this.dispatchEvent(new Event('change'));
    }

    subscribe(listener) {
        this.addEventListener('change', listener);
        return () => this.removeEventListener('change', listener);
    }

    setSession(token, user) {
        this.#commit({ token, user });
    }

    clearSession() {
        this.#commit({ token: null, user: null, cart: [], activeOrderId: null });
    }

    addToCart(product, quantity) {
        const cart = this.#state.cart.map(line => ({ ...line }));
        const existing = cart.find(line => line.productId === product.id);
        if (existing) {
            existing.quantity += quantity;
        } else {
            cart.push({ productId: product.id, name: product.name, price: product.price, quantity });
        }
        this.#commit({ cart });
    }

    removeFromCart(productId) {
        this.#commit({ cart: this.#state.cart.filter(line => line.productId !== productId) });
    }

    clearCart() {
        this.#commit({ cart: [] });
    }

    setActiveOrder(activeOrderId) {
        this.#commit({ activeOrderId });
    }
}

export const store = new Store();
