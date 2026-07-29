import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  typescript: {
    ignoreBuildErrors: true,
  },
  reactStrictMode: false,
  allowedDevOrigins: ["*.space-z.ai"],
  images: {
    remotePatterns: [
      { protocol: "https", hostname: "images.unsplash.com" },
      { protocol: "https", hostname: "plus.unsplash.com" },
      { protocol: "https", hostname: "source.unsplash.com" },
      { protocol: "https", hostname: "z.ai" },
    ],
  },
  async rewrites() {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5161";
    return [
      {
        source: "/api/:path*",
        destination: `${apiUrl}/api/:path*`,
      },
      {
        source: "/swagger/:path*",
        destination: `${apiUrl}/swagger/:path*`,
      },
      {
        source: "/hangfire/:path*",
        destination: `${apiUrl}/hangfire/:path*`,
      },
      {
        source: "/hubs/:path*",
        destination: `${apiUrl}/hubs/:path*`,
      },
    ];
  },
};

export default nextConfig;
