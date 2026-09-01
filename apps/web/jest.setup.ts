import "@testing-library/jest-dom";

// Minimal next/navigation stub so client components that read the router
// in useEffect can render under jest without a Next.js host.
jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    refresh: jest.fn(),
    back: jest.fn(),
    forward: jest.fn(),
    prefetch: jest.fn(),
  }),
  usePathname: () => "/",
  useSearchParams: () => new URLSearchParams(),
}));

beforeEach(() => {
  window.localStorage.clear();
  // Default to a "signed-in" state so DashboardView doesn't redirect.
  window.localStorage.setItem("serenauth.token", "test-token");
  window.localStorage.setItem(
    "serenauth.user",
    JSON.stringify({
      email: "test@example.com",
      displayName: "Test User",
      role: "Admin",
      organizationId: "test-org",
    }),
  );
});
