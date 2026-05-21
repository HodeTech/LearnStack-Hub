export default function LoginPage() {
  return (
    <section className="mx-auto max-w-md px-6 py-24">
      <h1 className="text-3xl font-semibold">LearnStack Hub</h1>
      <p className="mt-4 text-hub-muted">
        Operator portal. Sign in with your `learnstack-hub` realm credentials.
      </p>
      <p className="mt-8 text-sm text-hub-muted">
        OIDC + PKCE login flow wires up in P02c-4 against the Keycloak
        `learnstack-hub` realm. MFA (TOTP) is required for every operator
        account.
      </p>
    </section>
  );
}
