import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  Cpu,
  Radio,
  RefreshCw,
  Wifi
} from "lucide-react";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import api from "../api/client";
import FollowUpPanel from "../components/FollowUpPanel";

const modules = [
  {
    title: "Telemetry",
    description:
      "Review readings, register sensors, and investigate anomalies.",
    path: "/telemetry",
    icon: Activity
  },
  {
    title: "Command Stream",
    description:
      "Queue commands, process priority requests, and review history.",
    path: "/commands",
    icon: Radio
  },
  {
    title: "Network Topology",
    description:
      "Inspect mesh connectivity, trace routes, and search history.",
    path: "/topology",
    icon: Wifi
  }
];

export default function OverviewDashboard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");
  const [updatedAt, setUpdatedAt] = useState(null);

  useEffect(() => {
    let active = true;

    async function loadOverview() {
      try {
        const [sensors, investigations, commands] =
          await Promise.all([
            api.get("/sensors"),
            api.get("/investigations"),
            api.get("/commands/summary")
          ]);

        if (!active) return;

        setData({
          sensors: sensors.data,
          investigations: investigations.data,
          commands: commands.data
        });
        setUpdatedAt(new Date());
      } catch (requestError) {
        if (active) setError(requestError.message);
      } finally {
        if (active) setLoading(false);
      }
    }

    loadOverview();

    return () => {
      active = false;
    };
  }, []);

  async function handleRefresh() {
    try {
      setWorking(true);
      setError("");

      const [sensors, investigations, commands] =
        await Promise.all([
          api.get("/sensors"),
          api.get("/investigations"),
          api.get("/commands/summary")
        ]);

      setData({
        sensors: sensors.data,
        investigations: investigations.data,
        commands: commands.data
      });
      setUpdatedAt(new Date());
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setWorking(false);
    }
  }

  const metrics = [
    ["Registered sensors", data?.sensors.length],
    ["Investigations", data?.investigations.length],
    [
      "Queued commands",
      data
        ? data.commands.criticalCommands +
          data.commands.normalCommands
        : null
    ],
    ["Completed commands", data?.commands.completedCommands]
  ];

  return (
    <main className="dashboard-page">
      <aside className="dashboard-sidebar">
        <Link to="/" className="dashboard-brand">
          <span className="brand-mark">
            <Cpu size={21} />
          </span>
          <span>
            <strong>Smart-X</strong>
            <small>Engineering Console</small>
          </span>
        </Link>

        <nav className="dashboard-nav">
          <span className="nav-label">Navigation</span>

          <Link
            to="/dashboard"
            className="nav-item nav-item--active"
          >
            <Cpu size={18} />
            Overview
          </Link>

          {modules.map((module) => {
            const Icon = module.icon;

            return (
              <Link
                key={module.path}
                to={module.path}
                className="nav-item"
              >
                <Icon size={18} />
                {module.title}
              </Link>
            );
          })}

          <Link to="/about" className="nav-item">
            About Smart-X
          </Link>
        </nav>
      </aside>

      <section className="dashboard-content">
        <header className="dashboard-header">
          <div>
            <Link to="/" className="back-link">
              <ArrowLeft size={15} />
              Home
            </Link>
            <h1>Operations Dashboard</h1>
            <p>
              Review current activity and open a management module.
            </p>
          </div>

          <button
            className="refresh-button"
            type="button"
            onClick={handleRefresh}
            disabled={loading || working}
          >
            <RefreshCw size={17} />
            {working ? "Refreshing…" : "Refresh overview"}
          </button>
        </header>

        {error && (
          <div className="dashboard-error" role="alert">
            <AlertTriangle size={18} />
            {error}
          </div>
        )}

        <p className="topology-note">
          {updatedAt
            ? `Last successful update: ${updatedAt.toLocaleString()}`
            : loading
              ? "Loading overview…"
              : "Overview data is unavailable. Try refreshing."}
        </p>

        <section className="metric-grid">
          {metrics.map(([label, value]) => (
            <article className="metric-card" key={label}>
              <span className="metric-icon">
                <Activity size={20} />
              </span>
              <div>
                <small>{label}</small>
                <strong>{value ?? "—"}</strong>
              </div>
            </article>
          ))}
        </section>

        <section
          className="overview-module-grid"
          aria-label="Management modules"
        >
            
          {modules.map((module) => {
            const Icon = module.icon;

            return (
              <article
                className="panel overview-module-card"
                key={module.path}
              >
                <span className="metric-icon">
                  <Icon size={24} />
                </span>
                <h2>{module.title}</h2>
                <p>{module.description}</p>
                <Link
                  to={module.path}
                  className="secondary-button"
                >
                  Open module
                </Link>
              </article>
            );
          })}
               </section>

        <FollowUpPanel sensors={data?.sensors || []} />
      </section>
    </main>
  );
}