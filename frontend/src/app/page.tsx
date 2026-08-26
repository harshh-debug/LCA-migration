import { StatusPanel } from "@/components/status-panel";

export default function Home() {
  return (
    <main>
      <p className="eyebrow">LCA migration · Sprint 1</p>
      <h1>Web and API foundation</h1>
      <p className="lede">
        This surface proves the new Next.js application can reach the versioned ASP.NET Core boundary.
        Legacy business routes and data remain under legacy ownership.
      </p>
      <StatusPanel />
    </main>
  );
}

