export const SiteFooter = () => (
  <footer className="border-t border-foreground/15 bg-background">
    <div className="mx-auto flex w-full max-w-[1800px] flex-col gap-4 px-4 py-6 text-sm text-muted-foreground sm:px-6 lg:px-8">
      <div>
        <p className="font-semibold uppercase tracking-[0.32em] text-foreground/80">
          Maltese Transcriber
        </p>
        <p className="mt-2 max-w-2xl uppercase tracking-[0.2em]">
          Transcribe Maltese speech and read English translation in one page.
        </p>
      </div>
    </div>
  </footer>
);
