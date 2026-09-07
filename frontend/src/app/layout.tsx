import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "LCA Administration",
  description: "Tenant and Platform Owner administration",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
