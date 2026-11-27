import { ArrowLeft, Cpu } from "lucide-react";
import { Link } from "react-router-dom";

export default function AboutPage() {
  return (
    <main className="landing-page">
      <header className="landing-header">
        <Link to="/" className="brand">
          <span className="brand-mark">
            <Cpu size={22} />
          </span>

          <span>
            <strong>Smart-X</strong>
            <small>IoT Mesh Ecosystem</small>
          </span>
        </Link>

        <Link to="/" className="back-link">
          <ArrowLeft size={15} />
          Home
        </Link>
      </header>

      <section className="hero-section about-hero">
        <div className="eyebrow">About the project</div>

        <h1>
          Understanding connected
          <span> environments.</span>
        </h1>

        <p>
          Smart-X is an academic IoT management prototype
          developed for PROG7312. It brings sensor readings,
          device commands, and mesh network analysis into
          one engineering console.
        </p>
      </section>

      <section className="about-grid">
        <article className="panel about-card">
          <span className="section-label">Purpose</span>
          <h2>Support operational decisions</h2>

          <p>
            Operators can review telemetry, investigate
            abnormal readings, queue commands, and explore
            how device failures affect gateway connectivity.
          </p>

          <p>
            Example environments include hydroponics
            facilities, smart buildings, and power monitoring.
          </p>
        </article>

        <article className="panel about-card">
          <span className="section-label">Capabilities</span>
          <h2>Three connected modules</h2>

          <ul>
            <li>
              Telemetry: register sensors, publish typed
              readings, and search historical data.
            </li>
            <li>
              Commands: manage priority requests,
              review history, and undo completed commands.
            </li>
            <li>
              Topology: inspect connections, trace gateway
              routes, and simulate offline nodes.
            </li>
          </ul>
        </article>

        <article className="panel about-card">
          <span className="section-label">Implementation</span>
          <h2>Data structures with a purpose</h2>

          <p>
            The application uses queues, stacks, dictionaries,
            sets, an AVL history tree, and a weighted graph.
            Breadth-first search finds routes with the fewest
            hops, while Prim’s algorithm builds a minimum
            spanning tree for the gateway’s connected component.
          </p>

          <p>
            A React interface communicates with an ASP.NET
            Core API, with SQL Server storing sensor,
            telemetry, attachment, and topology records.
          </p>
        </article>

        <article className="panel about-card">
          <span className="section-label">Prototype scope</span>
          <h2>Simulation and interpretation</h2>

          <p>
            Telemetry can be submitted through the simulator.
            Command processing records simulated completion;
            it does not confirm execution on physical hardware.
          </p>

          <p>
            Network energy costs are demonstration values.
            Recommendations use programmed rules, and their
            confidence scores are heuristic indicators.
          </p>
        </article>
      </section>

      <div className="about-actions">
        <Link to="/telemetry" className="primary-button">
          Explore telemetry
        </Link>

        <Link to="/topology" className="secondary-button">
          Explore network topology
        </Link>
      </div>

      <footer className="landing-footer">
        <span>Smart-X Engineering Console</span>
        <span>PROG7312 · Academic project</span>
      </footer>
    </main>
  );
}