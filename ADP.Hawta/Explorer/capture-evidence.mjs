import {writeFile} from 'node:fs/promises';
import {captureEvidence} from './evidence.mjs';

const evidence = await captureEvidence();
await writeFile(new URL('evidence.json', import.meta.url), JSON.stringify(evidence, null, 2) + '\n');
console.log('Captured ' + evidence.entries.length + ' public source excerpts. Review the snapshot diff before committing.');
