import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Emits a self-contained production server for the Docker runtime image.
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/api/hubs/:path*",
        destination: "http://api:8080/hubs/:path*",
      },
    ];
  },
};

export default nextConfig;
