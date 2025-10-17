(async function () {
  const href = 'https://fonts.googleapis.com/css2?family=Noto+Kufi+Arabic:wght@100..900&family=Nunito:ital,wght@0,200..1000;1,200..1000&display=swap';
  if (!document.querySelector(`link[href="${href}"]`)) {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
    console.log('✅ Noto Kufi Arabic and Nunito Fonts loaded globally.');
  }

  if (!window['blazorInvoke']) {
    window['blazorInvoke'] = async function (selector, functionName, ...args) {
      const element = document.querySelector(selector);
      if (!element) {
        console.error(`Element with selector "${selector}" not found.`);
        return;
      }

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
