import type { Metadata } from "next";
import type { ReactNode } from "react";

import "./globals.css";

export const metadata: Metadata = {
  title: "LearnStack Hub",
  description:
    "LearnStack Hub operator portal — tenant lifecycle, plans, entitlements, custom domains, license keys.",
};

type RootLayoutProps = {
  readonly children: ReactNode;
};

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html lang="en">
      <body className="bg-hub-bg text-hub-fg font-sans antialiased">
        {children}
      </body>
    </html>
  );
}
