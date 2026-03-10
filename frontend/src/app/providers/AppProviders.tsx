import type { PropsWithChildren } from "react";
import { ThemeProvider } from "next-themes";

import { Toaster } from "@/components/ui/sonner";

export const AppProviders = ({ children }: PropsWithChildren) => (
  <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
    {children}
    <Toaster />
  </ThemeProvider>
);
