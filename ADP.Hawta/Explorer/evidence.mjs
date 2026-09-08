import {readFile, realpath} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve, relative, sep} from 'node:path';
import {fileURLToPath} from 'node:url';
import {evidenceSources} from './evidence-sources.mjs';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
export const hashAlgorithm = 'sha256-lf-v1';
const sourcesById = new Map(evidenceSources.map(source => [source.id, source]));

// Git checkouts may use LF or CRLF. Compare the full UTF-8 source after only
// normalizing CRLF to LF, so a checkout's line-ending setting is not source drift.
export const normalizeSource = text => text.replace(/\r\n/g, '\n');
export const sourceHash = text => createHash('sha256').update(normalizeSource(text), 'utf8').digest('hex');

export async function readSource(id, root = repositoryRoot) {
  const source = sourcesById.get(id);
  if (!source) throw new Error('Source is not in the evidence catalog');
  const canonicalRoot = await realpath(root);
  const canonicalFile = await realpath(resolve(canonicalRoot, source.path));
  // Reject links/junctions that redirect a catalog entry to another source or
  // outside this checkout. No request or captured path can choose a file to read.
  if (relative(canonicalRoot, canonicalFile).split(sep).join('/') !== source.path) {
    throw new Error('Source resolves outside its catalog location');
  }
  return normalizeSource(await readFile(canonicalFile, 'utf8'));
}

export async function captureEvidence(root = repositoryRoot) {
  const entries = await Promise.all(evidenceSources.map(async source => {
    const text = await readSource(source.id, root);
    const lines = text.split('\n');
    if (source.start < 1 || source.end > lines.length || source.end < source.start) {
      throw new Error('Review the evidence range for ' + source.id);
    }
    return {...source, sha256: sourceHash(text), excerpt: lines.slice(source.start - 1, source.end).join('\n')};
  }));
  return {
    schemaVersion: 1, capturedAt: new Date().toISOString(), hashAlgorithm,
    basis: 'Public ADP source excerpts. Paths are relative to the repository root. Full-source hashes normalize CRLF to LF. Deployment and pipeline benchmarks are not verified by this Explorer.',
    entries,
  };
}

export async function checkEvidence(evidence, root = repositoryRoot) {
  const checks = await Promise.all(evidence.entries.map(async entry => {
    const source = sourcesById.get(entry.id);
    let status = 'unavailable';
    // Invalid or outdated catalog references cannot be used as filesystem paths.
    if (evidence.hashAlgorithm === hashAlgorithm && source && entry.path === source.path &&
        entry.start === source.start && entry.end === source.end && /^[a-f0-9]{64}$/.test(entry.sha256)) {
      try {
        status = sourceHash(await readSource(source.id, root)) === entry.sha256 ? 'unchanged' : 'changed';
      } catch { /* Missing or redirected source stays unavailable. */ }
    }
    return {id: entry.id, sha256: entry.sha256, status};
  }));
  return {capturedAt: evidence.capturedAt, hashAlgorithm: evidence.hashAlgorithm, checkedAt: new Date().toISOString(), checks};
}
