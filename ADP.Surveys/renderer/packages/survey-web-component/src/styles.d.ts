/** Vite's `?inline` query returns CSS as a string at build time. Declare the
 *  module shape so TS resolves the import. */
declare module '*.css?inline' {
  const content: string;
  export default content;
}
