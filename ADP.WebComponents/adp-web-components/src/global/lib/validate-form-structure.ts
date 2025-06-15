import { StructureArray, StructureObject } from '~types/forms';

function parseElement(str: string): StructureObject {
  const elementPart = str.split(/\.|#/)[0];
  const classMatch = str.match(/\.(\w+)/);
  const idMatch = str.match(/#(\w+)/);

  return {
    children: [],
    element: elementPart,
    id: idMatch ? idMatch[1] : '',
    class: classMatch ? classMatch[1] : '',
  };
}

const recursiveParser = (structuredArray: StructureArray): StructureObject => {
  if (!Array.isArray(structuredArray) || !structuredArray.length) throw new Error('Invalid structure');

  let parent: StructureObject = { element: '', class: '', id: '', children: [] };

  let startIndex = 0;

  if (typeof structuredArray[0] === 'string' && structuredArray[0].startsWith('div')) {
    parent = parseElement(structuredArray[0]);
    startIndex++;
  }

  for (let idx = startIndex; idx < structuredArray.length; idx++) {
    const child = structuredArray[idx];

    if (typeof child === 'string') parent.children.push(parseElement(child));
    else parent.children.push(recursiveParser(child));
  }

  return parent;
};

export const formStructureStringExample = '[["div.kodo#j7","name.name#name"], "div.d3#s2"]';

export const isValidStructure = (requestedStructure: string | StructureArray): StructureObject => {
  try {
    const parsedStructure: StructureArray = typeof requestedStructure === 'string' ? JSON.parse(requestedStructure) : requestedStructure;

    if (!Array.isArray(parsedStructure) || !parsedStructure.length) throw new Error('Invalid structure');

    const structuredObject = recursiveParser(parsedStructure);

    return structuredObject;
  } catch (error) {
    console.log(error);

    return null;
  }
};
