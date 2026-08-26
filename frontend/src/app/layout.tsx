import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "LCA Migration Foundation",
  description: "Sprint 1 web and backend migration foundation",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}

