import type { Metadata } from "next";
import { Geist, Geist_Mono, Fraunces } from "next/font/google";
import "./globals.css";
import { Toaster } from "sonner";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const fraunces = Fraunces({
  variable: "--font-fraunces",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "FarmClaim — AI-Powered Crop Insurance",
  description:
    "Protect your harvest with intelligent, AI-driven crop insurance. File claims, manage policies, and get paid faster with FarmClaim.",
  keywords: [
    "crop insurance",
    "farm insurance",
    "agriculture",
    "AI claims",
    "FarmClaim",
  ],
  authors: [{ name: "FarmClaim" }],
  icons: {
    icon: "/favicon.svg",
  },
  openGraph: {
    title: "FarmClaim — AI-Powered Crop Insurance",
    description:
      "Protect your harvest with intelligent, AI-driven crop insurance.",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body
        className={`${geistSans.variable} ${geistMono.variable} ${fraunces.variable} antialiased bg-background text-foreground`}
      >
        {children}
        <Toaster position="top-right" richColors />
      </body>
    </html>
  );
}
