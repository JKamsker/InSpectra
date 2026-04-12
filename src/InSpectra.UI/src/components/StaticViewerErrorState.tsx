interface StaticViewerErrorStateProps {
  error: string | null;
  message?: string;
}

export function StaticViewerErrorState({ error, message }: StaticViewerErrorStateProps) {
  return (
    <main className="ds-content-screen">
      <section className="ds-hero-panel panel">
        <div className="eyebrow">InSpectraUI</div>
        <h1>Failed to load</h1>
        {error && <p className="ds-inline-alert" role="alert">{error}</p>}
        {message && <p className="ds-inline-alert" role="alert">{message}</p>}
      </section>
    </main>
  );
}
