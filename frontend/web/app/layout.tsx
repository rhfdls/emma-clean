import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { BillingProvider } from "@/context/BillingContext";
import { SessionProvider } from "@/context/SessionContext";
import { OrgProvider } from "@/context/OrgContext";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "EMMA Onboarding",
  description: "Sign up, choose a plan, and get started",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={`${geistSans.variable} ${geistMono.variable} antialiased`}>
        <SessionProvider>
          <OrgProvider>
            <BillingProvider>
              {children}
            </BillingProvider>
          </OrgProvider>
        </SessionProvider>
      </body>
    </html>
  );
}
