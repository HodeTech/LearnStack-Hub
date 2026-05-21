import type { ReactNode } from "react";

// TODO(2026-05-21, @platform, phase-02c-4): replace this placeholder layout
// with the operator-portal chrome: top nav (tenant list / plans / custom
// domains / licenses / audit / operator profile), MFA-required auth guard,
// and a `learnstack-hub` realm token check that 401s the entire route group
// if the realm claim is wrong.

type AuthenticatedLayoutProps = {
  readonly children: ReactNode;
};

export default function AuthenticatedLayout({
  children,
}: AuthenticatedLayoutProps) {
  return <div className="min-h-screen bg-hub-bg">{children}</div>;
}
