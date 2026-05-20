"use client";

import { ApolloProvider } from "@apollo/client";
import { useMemo } from "react";
import { makeApolloClient } from "@/lib/apollo";

export function ApolloProviderClient({
  children,
}: {
  children: React.ReactNode;
}) {
  const client = useMemo(() => makeApolloClient(), []);
  return <ApolloProvider client={client}>{children}</ApolloProvider>;
}
