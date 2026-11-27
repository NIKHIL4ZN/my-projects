import {
  Activity,
  ArrowRight,
  Cpu,
  Network,
  RadioTower,
  ShieldCheck
} from "lucide-react";
import { Link } from "react-router-dom";

const pillars = [
  {
    title: "Sensor Data Ingestion",
    subtitle: "Telemetry Gateway",
    description:
      "Register ESP32 devices, ingest strongly typed telemetry, " +
      "inspect sensor history, and resolve live anomalies.",
    icon: Activity,
    status: "Active",
    path: "/telemetry",
    className: "pillar-card pillar-card--active"
  },
  {
    title: "Real-Time Command Stream",
    subtitle: "Commands and History",
    description:
      "Queue device commands, process priority requests, review " +
      "command history, and investigate predictive recommendations.",
    icon: RadioTower,
    status: "Active",
    path: "/commands",
    className: "pillar-card pillar-card--active"
  },
  {
  title: "Network Topology",
  subtitle: "Mesh Routing",
  description:
    "Visualise mesh connections, inspect connectivity, trace " +
    "gateway routes, and simulate device failures.",
  icon: Network,
  status: "Active",
  path: "/topology",
  className: "pillar-card pillar-card--active"
}
];

export default function LandingPage() {
  return (
    <main className="landing-page">
      <div className="ambient ambient--one" />
      <div className="ambient ambient--two" />

      <header className="landing-header">
        <Link
          to="/"
          className="brand"
          aria-label="Smart-X home"
        >
          <span className="brand-mark">
            <Cpu size={22} />
          </span>

          <span>
            <strong>Smart-X</strong>
            <small>IoT Mesh Ecosystem</small>
          </span>
        </Link>

       <nav className="landing-links" aria-label="Main navigation">
  <Link to="/dashboard" className="secondary-button">
    Open dashboard
  </Link>

  <Link to="/about" className="back-link">
    About Smart-X
  </Link>
</nav>
      </header>

      <section className="hero-section">
        <div className="eyebrow">
          <ShieldCheck size={16} />
          Secure telemetry orchestration
        </div>

        <h1>
          One gateway for every
          <span> connected environment.</span>
        </h1>

        <p>
          Monitor distributed IoT sensors, process strongly typed
          telemetry, manage device commands, and investigate abnormal
          device behaviour from one responsive management suite.
        </p>

        <div className="architecture-summary">
          <span>.NET 10 API</span>
          <span>React</span>
          <span>SQL Server</span>
          <span>AES-256 storage</span>
        </div>
      </section>

      <section
        className="pillar-grid"
        aria-label="Smart-X modules"
      >
        {pillars.map((pillar, index) => {
          const Icon = pillar.icon;

          const content = (
            <>
              <div className="pillar-number">
                0{index + 1}
              </div>

              <div className="pillar-icon">
                <Icon size={25} />
              </div>

              <div className="pillar-copy">
                <span className="pillar-subtitle">
                  {pillar.subtitle}
                </span>

                <h2>{pillar.title}</h2>

                <p>{pillar.description}</p>
              </div>

              <div className="pillar-footer">
                <span
                  className={
                    pillar.disabled
                      ? "module-status module-status--locked"
                      : "module-status module-status--active"
                  }
                >
                  {pillar.status}
                </span>

                {!pillar.disabled && (
                  <span className="open-module">
                    Open module
                    <ArrowRight size={17} />
                  </span>
                )}
              </div>
            </>
          );

          if (pillar.disabled) {
            return (
              <article
                className={pillar.className}
                key={pillar.title}
                aria-disabled="true"
              >
                {content}
              </article>
            );
          }

          return (
            <Link
              className={pillar.className}
              to={pillar.path}
              key={pillar.title}
            >
              {content}
            </Link>
          );
        })}
      </section>

      <footer className="landing-footer">
        <span>
          Smart-X Engineering Console
        </span>

        <span>
         Telemetry · Command management · Mesh routing
        </span>
      </footer>
    </main>
  );
}