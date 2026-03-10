import "@testing-library/jest-dom";

Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => {},
  }),
});

Object.defineProperty(globalThis, "crypto", {
  value: {
    randomUUID: () => "test-session-id",
  },
  configurable: true,
});

Object.assign(navigator, {
  clipboard: {
    writeText: async () => Promise.resolve(),
  },
});
