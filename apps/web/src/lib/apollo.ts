"use client";

import {
  ApolloClient,
  InMemoryCache,
  HttpLink,
  from,
} from "@apollo/client";
import { setContext } from "@apollo/client/link/context";

const httpLink = new HttpLink({
  uri:
    process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT ??
    "http://localhost:8080/graphql",
});

// Demo-grade token store. Production: replace with OAuth/OIDC.
function readToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem("serenauth.token");
}

const authLink = setContext((_, { headers }) => {
  const token = readToken();
  return {
    headers: {
      ...headers,
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
  };
});

export function makeApolloClient() {
  return new ApolloClient({
    link: from([authLink, httpLink]),
    cache: new InMemoryCache(),
    defaultOptions: {
      watchQuery: { fetchPolicy: "cache-and-network" },
    },
  });
}
