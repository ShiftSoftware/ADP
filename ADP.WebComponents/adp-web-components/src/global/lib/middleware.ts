(async function () {
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
