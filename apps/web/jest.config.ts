import type { Config } from "jest";

const config: Config = {
  testEnvironment: "jsdom",
  setupFilesAfterEach: ["<rootDir>/jest.setup.ts"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/src/$1",
    "^@serenauth/shared-types$": "<rootDir>/../../packages/shared-types/src/index.ts",
    "\\.(css|less|scss)$": "<rootDir>/jest.css-stub.ts"
  },
  transform: {
    "^.+\\.tsx?$": ["ts-jest", { tsconfig: "<rootDir>/tsconfig.jest.json" }]
  },
  collectCoverageFrom: [
    "src/**/*.{ts,tsx}",
    "!src/app/**/layout.tsx",
    "!src/app/**/page.tsx"
  ],
  coverageThreshold: {
    global: { branches: 85, functions: 85, lines: 85, statements: 85 }
  }
};

export default config;
