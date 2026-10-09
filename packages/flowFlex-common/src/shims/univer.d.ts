/**
 * Ambient types for Univer modules that only exist inside the pnpm virtual store
 * (`node_modules/.pnpm/node_modules/@univerjs/*`).
 *
 * Vite resolves these specifiers at runtime, but TypeScript's node resolution only
 * walks real `node_modules` directories, so it reports TS2307 for them.
 */

// Locale packs — plain data modules, no type declarations published
declare module '@univerjs/preset-sheets-core/locales/en-US';
declare module '@univerjs/preset-docs-core/locales/en-US';

// Low-level packages used by the Slides editor (only loaded on the Slide route)
declare module '@univerjs/engine-render';
declare module '@univerjs/ui';
declare module '@univerjs/docs';
declare module '@univerjs/docs-ui';
declare module '@univerjs/drawing';
declare module '@univerjs/slides';
declare module '@univerjs/slides-ui';
