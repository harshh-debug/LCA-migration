"use client";

import { FormEvent, useState } from "react";

export function ForgotPassword({ platform = false }: { platform?: boolean }) {
  const [email, setEmail] = useState(""); const [message, setMessage] = useState(""); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(""); setError("");
    try {
      const response = await fetch(platform ? "/api/v1/platform/auth/forgot-password" : "/api/v1/auth/forgot-password", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email }),
      });
      if (response.status === 429) throw new Error("Too many recovery attempts. Please wait and try again.");
      if (!response.ok) throw new Error("Password recovery is temporarily unavailable.");
      setMessage("If the account is eligible, a password reset email has been sent.");
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Password recovery is temporarily unavailable."); } finally { setBusy(false); }
  }
  const login = platform ? "/platform/login" : "/";
  return <main className="loginShell"><section className="loginIntro"><p className="eyebrow">Account recovery</p><h1>Reset your password securely.</h1><p>We will send a short-lived reset link to the registered email address.</p></section><form className="loginForm" onSubmit={submit}><h2>Forgot password</h2><label>Email<input type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} /></label>{error && <p className="alertInline" role="alert">{error}</p>}{message && <p className="successMessage">{message}</p>}<button disabled={busy}>{busy ? "Submitting…" : "Send reset email"}</button><a href={login}>Return to sign in</a></form></main>;
}

export function CompletePassword({ platform = false, initialSetup = false }: { platform?: boolean; initialSetup?: boolean }) {
  const [password, setPassword] = useState(""); const [confirm, setConfirm] = useState(""); const [message, setMessage] = useState(""); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(""); setMessage("");
    if (password !== confirm) { setError("Passwords do not match."); return; }
    const parameters = new URLSearchParams(window.location.search); const userId = parameters.get("userId"); const token = parameters.get("token");
    if (!userId || !token) { setError("This password link is invalid or incomplete."); return; }
    const path = initialSetup ? "/api/v1/auth/initial-password-setup" : platform ? "/api/v1/platform/auth/reset-password" : "/api/v1/auth/reset-password";
    setBusy(true);
    try {
      const response = await fetch(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ userId, token, newPassword: password }) });
      if (!response.ok) { const problem = await response.json().catch(() => null) as { title?: string } | null; throw new Error(problem?.title ?? "The password link is invalid or has expired."); }
      setMessage(initialSetup ? "Your account is ready. You can now sign in." : "Your password has been reset. Sign in again with the new password.");
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to update the password."); } finally { setBusy(false); }
  }
  const login = platform ? "/platform/login" : "/";
  return <main className="loginShell"><section className="loginIntro"><p className="eyebrow">{initialSetup ? "Account setup" : "Account recovery"}</p><h1>{initialSetup ? "Choose your password." : "Create a new password."}</h1><p>The link is single-use. After completion, sign in normally.</p></section><form className="loginForm" onSubmit={submit}><h2>{initialSetup ? "Set up account" : "Reset password"}</h2><label>New password<input type="password" autoComplete="new-password" minLength={12} required value={password} onChange={(event) => setPassword(event.target.value)} /></label><label>Confirm password<input type="password" autoComplete="new-password" minLength={12} required value={confirm} onChange={(event) => setConfirm(event.target.value)} /></label>{error && <p className="alertInline" role="alert">{error}</p>}{message && <p className="successMessage">{message} <a href={login}>Sign in</a></p>}<button disabled={busy || Boolean(message)}>{busy ? "Saving…" : "Save password"}</button></form></main>;
}
