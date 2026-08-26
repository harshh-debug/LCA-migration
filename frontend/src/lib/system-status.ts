export type SystemStatus = {
  service: string;
  status: "ok";
  version: string;
  timestampUtc: string;
  correlationId: string;
};

export type SystemStatusResult =
  | { state: "available"; data: SystemStatus }
  | { state: "unavailable"; message: string };

type Fetcher = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export async function fetchSystemStatus(fetcher: Fetcher = fetch): Promise<SystemStatusResult> {
  try {
    const response = await fetcher("/api/v1/system/status", {
      cache: "no-store",
      headers: {
        Accept: "application/json",
      },
    });

    if (!response.ok) {
      return { state: "unavailable", message: `API returned HTTP ${response.status}.` };
    }

    const body: unknown = await response.json();
    if (!isSystemStatus(body)) {
      return { state: "unavailable", message: "API returned an invalid status response." };
    }

    return { state: "available", data: body };
  } catch {
    return { state: "unavailable", message: "API could not be reached." };
  }
}

function isSystemStatus(value: unknown): value is SystemStatus {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return candidate.status === "ok"
    && typeof candidate.service === "string"
    && typeof candidate.version === "string"
    && typeof candidate.timestampUtc === "string"
    && typeof candidate.correlationId === "string";
}

