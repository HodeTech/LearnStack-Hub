import type { Config } from "tailwindcss";

import { learnstackHubTailwindPreset } from "@learnstack-hub/config/tailwind";

const config: Config = {
  presets: [learnstackHubTailwindPreset],
  content: ["./src/**/*.{ts,tsx,mdx}"],
};

export default config;
