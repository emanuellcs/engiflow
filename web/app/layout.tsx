import type { Metadata } from "next";
import type { ReactNode } from "react";
import { cookies } from "next/headers";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import Providers from "@/app/providers";
import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";
import "katex/dist/katex.min.css";
import "./globals.css";

export const metadata: Metadata = {
  title: "EngiFlow",
  description: "Engineering change management for B2B teams.",
};

/**
 * Root application layout that installs the Material UI App Router cache,
 * global theme providers, Roboto font faces, and shared browser CSS.
 *
 * @param props - Layout properties supplied by the Next.js App Router.
 * @param props.children - The public or authenticated route subtree to render.
 * @returns The document shell shared by every EngiFlow web route.
 */
export default async function RootLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  const cookieStore = await cookies();
  const locale = cookieStore.get("engi-locale")?.value || "en";

  return (
    <html lang={locale}>
      <body>
        <AppRouterCacheProvider options={{ key: "mui" }}>
          <Providers initialLocale={locale as "en" | "pt-BR"}>{children}</Providers>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
