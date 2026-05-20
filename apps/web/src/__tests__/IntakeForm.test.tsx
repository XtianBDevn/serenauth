import { render, screen, fireEvent } from "@testing-library/react";
import { IntakeForm } from "../components/IntakeForm";

describe("IntakeForm", () => {
  it("shows validation messages on empty submit", () => {
    render(<IntakeForm />);
    fireEvent.click(screen.getByRole("button", { name: /request walkthrough/i }));
    expect(screen.getByText(/practice name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/contact name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/email is required/i)).toBeInTheDocument();
    expect(screen.getByText(/estimate is required/i)).toBeInTheDocument();
  });

  it("flags malformed emails", () => {
    render(<IntakeForm />);
    fireEvent.change(screen.getByPlaceholderText("Riverbend Dialysis"), {
      target: { value: "Riverbend" },
    });
    fireEvent.change(screen.getByPlaceholderText("Avery Carter"), {
      target: { value: "Avery" },
    });
    fireEvent.change(screen.getByPlaceholderText("avery@riverbend.example"), {
      target: { value: "not-an-email" },
    });
    fireEvent.change(screen.getByPlaceholderText("120"), {
      target: { value: "100" },
    });
    fireEvent.click(screen.getByRole("button", { name: /request walkthrough/i }));
    expect(screen.getByText(/enter a valid email/i)).toBeInTheDocument();
  });

  it("transitions to a success state on valid submission", () => {
    render(<IntakeForm />);
    fireEvent.change(screen.getByPlaceholderText("Riverbend Dialysis"), {
      target: { value: "Riverbend" },
    });
    fireEvent.change(screen.getByPlaceholderText("Avery Carter"), {
      target: { value: "Avery" },
    });
    fireEvent.change(screen.getByPlaceholderText("avery@riverbend.example"), {
      target: { value: "a@b.co" },
    });
    fireEvent.change(screen.getByPlaceholderText("120"), {
      target: { value: "150" },
    });
    fireEvent.click(screen.getByRole("button", { name: /request walkthrough/i }));
    expect(screen.getByText(/we'll be in touch/i)).toBeInTheDocument();
  });
});
