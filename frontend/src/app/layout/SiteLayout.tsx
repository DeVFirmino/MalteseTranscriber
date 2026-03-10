import { Outlet } from "react-router-dom";

import { SiteFooter } from "@/components/site/SiteFooter";
import { SiteHeader } from "@/components/site/SiteHeader";

export const SiteLayout = () => (
  <div className="relative min-h-screen overflow-x-hidden bg-background text-foreground">
    <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--foreground)/0.035)_1px,transparent_1px),linear-gradient(to_bottom,hsl(var(--foreground)/0.035)_1px,transparent_1px)] bg-[size:36px_36px]" />
    <div className="relative flex min-h-screen flex-col">
      <SiteHeader />
      <main className="flex-1">
        <Outlet />
      </main>
      <SiteFooter />
    </div>
  </div>
);
