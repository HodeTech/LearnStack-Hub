import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  transpilePackages: ["@learnstack-hub/ui", "@learnstack-hub/sdk"],
};

export default nextConfig;
