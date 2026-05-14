import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Emits a self-contained production server for the Docker runtime image.
  output: "standalone",
};

export default nextConfig;
