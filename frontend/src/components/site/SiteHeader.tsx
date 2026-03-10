import { Link } from "react-router-dom";

import { ThemeToggle } from "@/components/site/ThemeToggle";

export const SiteHeader = () => (
  <header className="sticky top-0 z-40 border-b border-foreground/15 bg-background/95 backdrop-blur-md">
    <div className="mx-auto flex w-full max-w-[1800px] flex-col gap-5 px-4 py-5 sm:px-6 lg:flex-row lg:items-center lg:justify-between lg:px-8">
      <div className="flex flex-col gap-4">
        <Link to="/" className="group inline-flex flex-col">
          <span className="text-[2.35rem] font-semibold uppercase leading-none tracking-[0.01em] text-foreground sm:text-[3.3rem]">
            Maltese<span className="text-primary">Transcriber</span>
          </span>
          <span className="mt-2 text-[11px] font-semibold uppercase tracking-[0.38em] text-muted-foreground">
            Click to transcribe Maltese to English
          </span>
        </Link>
      </div>

      <div className="flex items-center justify-between gap-3">
        <div className="inline-flex items-center gap-3 border border-foreground/15 px-4 py-3 text-sm font-semibold uppercase tracking-[0.32em] text-foreground">
          <span className="h-3.5 w-3.5 rounded-full bg-foreground/70" aria-hidden="true" />
          Session
        </div>
        <ThemeToggle />
      </div>
    </div>
  </header>
);
