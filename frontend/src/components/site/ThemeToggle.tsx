import { LaptopMinimal, MoonStar, Sun } from "lucide-react";
import { useTheme } from "next-themes";

import { cn } from "@/lib/utils";

const themeOptions = [
  { value: "light", label: "Light", icon: Sun },
  { value: "dark", label: "Dark", icon: MoonStar },
  { value: "system", label: "System", icon: LaptopMinimal },
] as const;

export const ThemeToggle = () => {
  const { theme = "system", setTheme } = useTheme();

  return (
    <div className="inline-flex items-center border border-foreground/15 bg-background">
      {themeOptions.map((option) => {
        const Icon = option.icon;
        const active = theme === option.value;

        return (
          <button
            key={option.value}
            type="button"
            onClick={() => setTheme(option.value)}
            className={cn(
              "inline-flex items-center gap-2 border-l border-foreground/15 px-3 py-3 text-xs font-semibold uppercase tracking-[0.24em] transition-colors first:border-l-0",
              active
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-foreground hover:text-background",
            )}
            aria-pressed={active}
          >
            <Icon className="h-3.5 w-3.5" />
            {option.label}
          </button>
        );
      })}
    </div>
  );
};
