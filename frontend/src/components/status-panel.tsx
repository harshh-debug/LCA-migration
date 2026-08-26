"use client";

import { useEffect, useState } from "react";
import { fetchSystemStatus, type SystemStatusResult } from "@/lib/system-status";

type ViewState = { state: "loading" } | SystemStatusResult;

export function StatusPanel() {
  const [result, setResult] = useState<ViewState>({ state: "loading" });

  useEffect(() => {
    let active = true;
    void fetchSystemStatus().then((nextResult) => {
      if (active) {
        setResult(nextResult);
      }
    });

    return () => {
      active = false;
    };
  }, []);

  if (result.state === "loading") {
    return <p className="status statusLoading">Checking the ASP.NET Core API…</p>;
  }

  if (result.state === "unavailable") {
    return (
      <section className="status statusUnavailable" aria-live="polite">
        <strong>API unavailable</strong>
        <span>{result.message}</span>
      </section>
    );
  }

  return (
    <section className="status statusAvailable" aria-live="polite">
      <strong>Foundation connected</strong>
      <span>{result.data.service} · version {result.data.version}</span>
      <small>Correlation {result.data.correlationId}</small>
    </section>
  );
}

