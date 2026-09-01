import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: {
          50: "#f0f7ff",
          100: "#dceeff",
          200: "#bcdcff",
          300: "#8bc4ff",
          400: "#56a4ff",
          500: "#2f86f0",
          600: "#1d6bd0",
          700: "#1855a8",
          800: "#16487f",
          900: "#143b62",
        },
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
      },
    },
  },
  plugins: [],
};

export default config;
