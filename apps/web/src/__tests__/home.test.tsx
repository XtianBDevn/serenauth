import { render, screen } from "@testing-library/react";
import HomePage from "../app/page";

describe("HomePage", () => {
  it("renders the calm hero headline", () => {
    render(<HomePage />);
    expect(
      screen.getByRole("heading", { name: /calm authorization\. faster care\./i }),
    ).toBeInTheDocument();
  });

  it("offers a path to request a demo", () => {
    render(<HomePage />);
    const ctas = screen.getAllByRole("link", { name: /request a demo/i });
    expect(ctas.length).toBeGreaterThanOrEqual(1);
  });
});
