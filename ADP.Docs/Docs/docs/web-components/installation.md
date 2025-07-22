# Installation

ADP Web Components offers two installation methods: **Bundle Installation** and **Standalone Installation**, depending on your project requirements. Both methods support **Latest Version CDN links** and **Versioned CDN links** for flexibility.

---

## Versioned vs. Latest CDN Links

ADP Web Components supports two CDN types:

- **Versioned CDN (Recommended):**
  Use a specific version of the ADP components library by specifying the version number after the `@` symbol in the CDN link (e.g., `@0.0.20`). This ensures stability as updates won’t affect your implementation.

- **Latest CDN:**
  Automatically uses the most recent updates when components are released. Specify `@latest` in the CDN link.

### Pros and Cons

!!! danger "Cons"

    - **Versioned CDN:** If bugs in your version are fixed in newer releases, you must manually update your version and code.
    - **Latest CDN:** Updates to components (e.g., changes in methods or callbacks) might break your code, requiring adjustments.

!!! success "Pros"

    - **Versioned CDN:** Ensures stability by locking in the version, avoiding breaking changes in newer releases.
    - **Latest CDN:** Automatically applies bug fixes and new features as they are released.

### Example CDN Links

=== "Versioned CDN (Recommended)"

    Bundle (e.g., 0.0.20):

    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.0.20/dist/shift-components/shift-components.esm.js"></script>
    ```

    Standalone (e.g., 0.0.20):

    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.0.20/dist/components/dynamic-claim.js"></script>
    ```

=== "Latest CDN"

    Bundle (Latest):

    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/shift-components/shift-components.esm.js"></script>
    ```

    Standalone (Latest):

    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/components/dynamic-claim.js"></script>
    ```

---

## Bundled vs. Standalone

Installation ADP Web Components offers two installation methods:

- **Bundled CDN:** Includes all components in one CDN. Useful for projects requiring multiple components. All components listed in [Component List](components/components-list.md) for more details.
- **Standalone CDN (Recommended):** Loads individual components, offering a lightweight and efficient solution for targeted use cases.

### Pros and Cons

!!! danger "Cons"

    - **Bundled CDN:** Larger file size compared to standalone components. Bundle are not lazy-loaded in most of the times but still its highly **recommended** to use `document.addEventListener('DOMContentLoaded', () => {})`.
    - **Standalone CDN:** Requires `document.addEventListener('DOMContentLoaded', () => {})` due to lazy-loaded components.

!!! success "Pros"

    - **Bundled CDN:** Convenient for projects using multiple components, as all components are imported together.
    - **Standalone CDN:** Lightweight, as only the required component is imported.

### Example Installation Links

=== "Bundled CDN"

    Latest:
    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/shift-components/shift-components.esm.js"></script>
    ```
    Versioned (e.g., 0.0.20):
    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.0.20/dist/shift-components/shift-components.esm.js"></script>
    ```

=== "Standalone CDN"

    Latest:
    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/components/dynamic-claim.js"></script>
    ```

    Versioned (e.g., 0.0.20):
    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.0.20/dist/components/dynamic-claim.js"></script>
    ```

---

## Usage

The usage of components remains consistent regardless of CDN type (Versioned vs. Latest) or installation method (Bundled vs. Standalone). However, using`document.addEventListener('DOMContentLoaded', () => {})` is **highly recommended** for reliable initialization.

=== "Bundle Usage"

    HTML code with DOMContentLoaded **Recommended**

    ```html

    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/shift-components/shift-components.esm.js"></script>

    <dynamic-redeem id="dynamic-redeem"></dynamic-redeem>
    <dynamic-claim is-dev="true" base-url="http://localhost:7174/api/secure-vehicle-lookup-test/" id="dynamic-claim"></dynamic-claim>

    <script>
      let dynamicClaim
      let dynamicRedeem

      document.addEventListener('DOMContentLoaded', () => {
        dynamicClaim = document.getElementById('dynamic-claim');
        dynamicRedeem = document.getElementById('dynamic-redeem');
        // Add your JavaScript logic here
      });
    </script>
    ```

    HTML code without DOMContentLoaded

    ```html

    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/shift-components/shift-components.esm.js"></script>

    <dynamic-redeem id="dynamic-redeem"></dynamic-redeem>
    <dynamic-claim is-dev="true" base-url="http://localhost:7174/api/secure-vehicle-lookup-test/" id="dynamic-claim"></dynamic-claim>

    <script>
        const dynamicClaim = document.getElementById('dynamic-claim');
        const dynamicRedeem = document.getElementById('dynamic-redeem');
    </script>
    ```

=== "Standalone Usage"

    Becarefull in Standing mode you have to import each component separately and its **mandatory** to use `DOMContentLoaded`

    ```html
    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/components/dynamic-claim.js"></script>

    <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/components/dynamic-redeem.js"></script>

    <dynamic-redeem id="dynamic-redeem"></dynamic-redeem>
    <dynamic-claim is-dev="true" base-url="http://localhost:7174/api/secure-vehicle-lookup-test/" id="dynamic-claim"></dynamic-claim>

    <script>
      let dynamicClaim
      let dynamicRedeem

      document.addEventListener('DOMContentLoaded', () => {
        dynamicClaim = document.getElementById('dynamic-claim');
        dynamicRedeem = document.getElementById('dynamic-redeem');
        // Add your JavaScript logic here
      });
    </script>
    ```

---

## Next Steps

- Explore the [Theming Guide](theming.md) for customization.
- Refer to the [Component List](components/components-list.md) for detailed documentation.
