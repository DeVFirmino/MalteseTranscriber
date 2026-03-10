import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import App from "@/App";

describe("App routes", () => {
  it("renders the transcriber workspace and actions", async () => {
    window.history.pushState({}, "", "/");

    render(<App />);

    expect(
      screen.getByRole("heading", {
        name: /transcribe maltese to english/i,
      }),
    ).toBeInTheDocument();

    expect(screen.getByRole("button", { name: /transcribe/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /clear/i }));
    expect(await screen.findByText(/waiting for audio/i)).toBeInTheDocument();
  });
});
