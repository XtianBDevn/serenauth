import { ApolloProviderClient } from "@/components/ApolloProviderClient";
import { LoginForm } from "@/components/LoginForm";

export const metadata = {
  title: "Sign in — SerenAuth",
};

export default function LoginPage() {
  return (
    <ApolloProviderClient>
      <LoginForm />
    </ApolloProviderClient>
  );
}
