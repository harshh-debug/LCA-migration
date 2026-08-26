"use client";

export default function ErrorPage({ reset }: { reset: () => void }) {
  return (
    <main>
      <p className="eyebrow">Unexpected frontend error</p>
      <h1>The foundation page could not be rendered.</h1>
      <button type="button" onClick={reset}>Try again</button>
    </main>
  );
}

