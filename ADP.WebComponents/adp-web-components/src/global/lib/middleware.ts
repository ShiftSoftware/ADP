(async function () {
  // Manrope is the vehicle-lookup family's Latin/Cyrillic face (lookup-tokens.css, --font-sans);
  // Noto Kufi Arabic its Arabic/Kurdish one and the legacy components'; Nunito the other
  // components' Latin face.
  // A host that sets its own fonts opts out with `window.adpWebComponentsFonts = false`, set before
  // the first component loads; the stylesheet is then never requested. Nothing else here depends on it.
  const href = 'https://fonts.googleapis.com/css2?family=Manrope:wght@200..800&family=Noto+Kufi+Arabic:wght@100..900&family=Nunito:ital,wght@0,200..1000;1,200..1000&display=swap';
  if (window['adpWebComponentsFonts'] !== false && !document.querySelector(`link[href="${href}"]`)) {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
    console.log('✅ Manrope, Noto Kufi Arabic and Nunito fonts loaded globally.');
  }

  if (!window['blazorInvoke']) {
    window['blazorInvoke'] = async function (selector, functionName, ...args) {
      const element = document.querySelector(selector);
      if (!element) {
        console.error(`Element with selector "${selector}" not found.`);
        return;
      }

      // A host can call before the component's code has loaded; wait for it rather than miss the call.
      const tag = element.tagName.toLowerCase();
      if (tag.includes('-') && !customElements.get(tag)) await customElements.whenDefined(tag);
      await element['componentOnReady']?.();

      if (typeof element[functionName] !== 'function') {
        console.error(`Function "${functionName}" not found on the element.`);
        return;
      }

      try {
        return await element[functionName](...args);
      } catch (error) {
        console.error(`Error invoking function "${functionName}" on element "${selector}":`, error);
      }
    };

    window['blazorInvokeSet'] = async function (selector, field, value) {
      const element = document.querySelector(selector);
      if (!element) {
        console.error(`Element with selector "${selector}" not found.`);
        return;
      }

      try {
        return (element[field] = value);
      } catch (error) {
        console.error(`Setting field ${field} failed to set value: ${value}:`, error);
      }
    };
    console.log('Global blazorInvoke initialized.');
  }
})();
