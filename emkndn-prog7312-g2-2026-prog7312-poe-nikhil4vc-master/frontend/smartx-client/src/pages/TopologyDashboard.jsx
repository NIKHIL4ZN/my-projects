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
import TelemetryHistoryPanel from "../components/TelemetryHistoryPanel";

function edgeKey(first, second) {
  return [first, second].sort().join(":");
}

export default function TopologyDashboard() {
  const [topology, setTopology] = useState({
    nodes: [],
    connections: []
  });
  const [gatewayId, setGatewayId] = useState("");
  const [offlineNodeId, setOfflineNodeId] = useState("");
  const [macAddress, setMacAddress] = useState("");
  const [view, setView] = useState("network");
  const [connectivity, setConnectivity] = useState(null);
  const [spanningTree, setSpanningTree] = useState(null);
  const [route, setRoute] = useState(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    async function loadInitialTopology() {
      try {
        const response = await api.get("/topology");

        if (!active) return;

        setTopology(response.data);
        setGatewayId(
          response.data.nodes.find(
            (node) => node.isGateway
          )?.id || ""
        );
      } catch (requestError) {
        if (active) setError(requestError.message);
      } finally {
        if (active) setLoading(false);
      }
    }

    loadInitialTopology();

    return () => {
      active = false;
    };
  }, []);

  async function handleRefresh() {
    try {
      setWorking(true);
      setError("");

      const response = await api.get("/topology");
      const nodes = response.data.nodes;

      setTopology(response.data);
      setGatewayId((current) =>
        nodes.some(
          (node) => node.id === current && node.isGateway
        )
          ? current
          : nodes.find((node) => node.isGateway)?.id || ""
      );
      setOfflineNodeId((current) =>
        nodes.some((node) => node.id === current)
          ? current
          : ""
      );
      clearAnalysis();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setWorking(false);
    }
  }

  function clearAnalysis() {
    setConnectivity(null);
    setSpanningTree(null);
    setRoute(null);
  }

  async function handleAnalyse(event) {
    event.preventDefault();

    try {
      setWorking(true);
      setError("");
      clearAnalysis();

      const params = offlineNodeId
        ? { offlineNodeId }
        : {};

      const [connectivityResponse, treeResponse] =
        await Promise.all([
          api.get(`/topology/connectivity/${gatewayId}`, {
            params
          }),
          api.get(`/topology/mst/${gatewayId}`, {
            params
          })
        ]);

      setConnectivity(connectivityResponse.data);
      setSpanningTree(treeResponse.data);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setWorking(false);
    }
  }

  async function handleTrace(event) {
    event.preventDefault();

    try {
      setWorking(true);
      setError("");
      setRoute(null);

      const response = await api.get(
        `/topology/route/${gatewayId}`,
        {
          params: {
            macAddress: macAddress.trim(),
            useSpanningTree: view === "mst",
            ...(offlineNodeId ? { offlineNodeId } : {})
          }
        }
      );

      setRoute(response.data);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setWorking(false);
    }
  }

  const gatewayNodes = topology.nodes.filter(
    (node) => node.isGateway
  );

  const unreachableIds = new Set(
    connectivity?.unreachableNodeIds || []
  );

  const highlightedEdges = new Set(
    (route?.path || []).slice(1).map((node, index) =>
      edgeKey(route.path[index].id, node.id)
    )
  );

  const visibleConnections =
    view === "mst"
      ? spanningTree?.connections || []
      : topology.connections.filter(
          (connection) => connection.isEnabled
        );

  const positions = new Map(
    topology.nodes.map((node, index) => {
      if (node.id === gatewayId) {
        return [node.id, { x: 450, y: 280 }];
      }

      const angle =
        (index / Math.max(topology.nodes.length, 1)) *
        Math.PI * 2;

      return [
        node.id,
        {
          x: 450 + Math.cos(angle) * 340,
          y: 280 + Math.sin(angle) * 215
        }
      ];
    })
  );

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
          <span className="nav-label">Modules</span>
          <Link to="/dashboard" className="nav-item">
  <Cpu size={18} />
  Overview
</Link>
          <Link to="/telemetry" className="nav-item">
            <Activity size={18} />
            Telemetry
            <small>Part 1</small>
          </Link>
          <Link to="/commands" className="nav-item">
            <Radio size={18} />
            Command Stream
            <small>Part 2</small>
          </Link>
          <Link
            to="/topology"
            className="nav-item nav-item--active"
          >
            <Wifi size={18} />
            Mesh Routing
            <small>PoE</small>
          </Link>
        </nav>

        <div className="sidebar-status">
          <div>
            <strong>
              {loading
                ? "Connecting…"
                : error
                  ? "Check connection"
                  : "Topology loaded"}
            </strong>
            <small>Mesh gateway</small>
          </div>
        </div>
      </aside>

      <section className="dashboard-content">
        <header className="dashboard-header">
          <div>
            <Link to="/" className="back-link">
              <ArrowLeft size={15} />
              All modules
            </Link>
            <h1>Network Topology</h1>
            <p>
              Inspect mesh connections, trace routes,
              and simulate node failures.
            </p>
          </div>
          <button
            className="refresh-button"
            type="button"
            onClick={handleRefresh}
            disabled={loading || working}
          >
            <RefreshCw size={17} />
            Refresh topology
          </button>
        </header>

        {error && (
          <div className="dashboard-error" role="alert">
            <AlertTriangle size={18} />
            {error}
          </div>
        )}

        <section className="metric-grid">
          {[
            ["Mesh nodes", topology.nodes.length],
            ["Enabled links", topology.connections.filter(
              (connection) => connection.isEnabled
            ).length],
            ["Unreachable nodes",
              connectivity?.unreachableNodeIds.length ?? "—"],
            ["MST energy cost",
              spanningTree?.totalEnergyCost ?? "—"]
          ].map(([label, value]) => (
            <article className="metric-card" key={label}>
              <span className="metric-icon">
                <Wifi size={20} />
              </span>
              <div>
                <small>{label}</small>
                <strong>{loading ? "—" : value}</strong>
              </div>
            </article>
          ))}
        </section>

        <section className="panel topology-panel">
          <div className="panel-heading">
            <div>
              <span className="section-label">
                Network analysis
              </span>
              <h2>Gateway and failure simulation</h2>
            </div>
          </div>

          <form
            className="topology-controls"
            onSubmit={handleAnalyse}
          >
            <label className="form-field">
              <span>Central gateway</span>
              <select
                value={gatewayId}
                disabled={working}
                required
                onChange={(event) => {
                  setGatewayId(event.target.value);
                  clearAnalysis();
                }}
              >
                <option value="">Select gateway</option>
                {gatewayNodes.map((node) => (
                  <option key={node.id} value={node.id}>
                    {node.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="form-field">
              <span>Simulate offline node</span>
              <select
                value={offlineNodeId}
                disabled={working}
                onChange={(event) => {
                  setOfflineNodeId(event.target.value);
                  clearAnalysis();
                }}
              >
                <option value="">No simulated failure</option>
                {topology.nodes.map((node) => (
                  <option key={node.id} value={node.id}>
                    {node.name}
                  </option>
                ))}
              </select>
            </label>

            <button
              className="primary-button"
              type="submit"
              disabled={loading || working || !gatewayId}
            >
              {working ? "Working…" : "Analyse network"}
            </button>
          </form>

          <p className="topology-note">
            Failure simulation applies only to these analysis
            requests. Saved node settings remain unchanged.
          </p>
        </section>

        <section className="panel topology-panel">
          <div className="panel-heading">
            <div>
              <span className="section-label">Mesh diagram</span>
              <h2>Connections and routes</h2>
            </div>
            <div className="history-controls">
              {[
                ["network", "Full network"],
                ["mst", "Minimum spanning tree"]
              ].map(([value, label]) => (
                <button
                  key={value}
                  className={
                    view === value
                      ? "history-toggle history-toggle--active"
                      : "history-toggle"
                  }
                  type="button"
                  disabled={
                    working ||
                    (value === "mst" && !spanningTree)
                  }
                  onClick={() => {
                    setView(value);
                    setRoute(null);
                  }}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div className="topology-diagram">
            <svg
              viewBox="0 0 900 560"
              role="img"
              aria-label="Mesh network diagram; node details are listed below"
            >
              {visibleConnections.map((connection) => {
                const first = positions.get(
                  connection.firstNodeId
                );
                const second = positions.get(
                  connection.secondNodeId
                );

                if (!first || !second) return null;

                const failed =
                  connection.firstNodeId === offlineNodeId ||
                  connection.secondNodeId === offlineNodeId ||
                  !topology.nodes.find(
                    (node) => node.id === connection.firstNodeId
                  )?.isOnline ||
                  !topology.nodes.find(
                    (node) => node.id === connection.secondNodeId
                  )?.isOnline;

                const highlighted = highlightedEdges.has(
                  edgeKey(
                    connection.firstNodeId,
                    connection.secondNodeId
                  )
                );

                return (
                  <g
                    key={edgeKey(
                      connection.firstNodeId,
                      connection.secondNodeId
                    )}
                  >
                    <line
                      x1={first.x}
                      y1={first.y}
                      x2={second.x}
                      y2={second.y}
                      className={
                        failed
                          ? "mesh-edge mesh-edge--failed"
                          : highlighted
                            ? "mesh-edge mesh-edge--route"
                            : "mesh-edge"
                      }
                    />
                    <text
                      x={(first.x + second.x) / 2}
                      y={(first.y + second.y) / 2 - 5}
                      className="mesh-cost"
                    >
                      {connection.energyCost}
                    </text>
                  </g>
                );
              })}

              {topology.nodes.map((node, index) => {
                const position = positions.get(node.id);
                const offline =
                  node.id === offlineNodeId || !node.isOnline;

                return (
                  <g key={node.id}>
                    <title>
                      {node.name} — {node.macAddress}
                    </title>
                    <circle
                      cx={position.x}
                      cy={position.y}
                      r={node.isGateway ? 24 : 19}
                      className={
                        offline
                          ? "mesh-node mesh-node--offline"
                          : node.isGateway
                            ? "mesh-node mesh-node--gateway"
                            : "mesh-node"
                      }
                    />
                    <text
                      x={position.x}
                      y={position.y + 5}
                      className="mesh-node-number"
                    >
                      {index + 1}
                    </text>
                    <text
                      x={position.x}
                      y={position.y + 39}
                      className="mesh-node-label"
                    >
                      {node.name.length > 24
                        ? `${node.name.slice(0, 21)}…`
                        : node.name}
                    </text>
                  </g>
                );
              })}
            </svg>
          </div>

          <p className="topology-note">
            Green: gateway. Red: offline node or unavailable link.
            Gold: traced route. Link labels show simulated energy costs.
          </p>

          {spanningTree && (
            <p className="topology-note">
              {spanningTree.spansEntireNetwork
                ? "The spanning tree connects the entire network."
                : "The tree covers the gateway’s connected component; some nodes are unreachable."}
              {" "}Its cost minimises the combined network links.
              Individual routes through it may have more hops.
            </p>
          )}

          <form
            className="topology-route-form"
            onSubmit={handleTrace}
          >
            <label className="form-field">
              <span>Trace device by MAC address</span>
              <input
                value={macAddress}
                onChange={(event) => {
                  setMacAddress(event.target.value);
                  setRoute(null);
                }}
                placeholder="A4:CF:12:9B:20:02"
                required
                disabled={working}
              />
            </label>
            <button
              className="secondary-button"
              type="submit"
              disabled={loading || working || !gatewayId}
            >
              Trace route
            </button>
          </form>

          {route && (
            <div className="topology-route-result" role="status">
              <strong>
                {route.isReachable
                  ? `${route.hopCount} hops · ${route.routeMode}`
                  : "Device cannot reach the gateway"}
              </strong>
              {route.isReachable && (
                <ol>
                  {route.path.map((node) => (
                    <li key={node.id}>{node.name}</li>
                  ))}
                </ol>
              )}
            </div>
          )}
        </section>

        <section className="panel sensor-panel topology-panel">
          <div className="panel-heading">
            <div>
              <span className="section-label">Device directory</span>
              <h2>Mesh nodes</h2>
            </div>
          </div>

          <div className="sensor-table-wrapper">
            <table className="sensor-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Node</th>
                  <th>MAC address</th>
                  <th>Availability</th>
                  <th>Gateway hops</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {topology.nodes.map((node, index) => (
                  <tr key={node.id}>
                    <td>{index + 1}</td>
                    <td>
                      <strong>{node.name}</strong>
                      <small className="mesh-node-id">
                        {node.id}
                      </small>
                    </td>
                    <td><code>{node.macAddress}</code></td>
                    <td>
                      {node.id === offlineNodeId
                        ? "Simulated offline"
                        : node.isOnline
                          ? "Online"
                          : "Offline"}
                    </td>
                    <td>
                      {connectivity
                        ? unreachableIds.has(node.id)
                          ? "Unreachable"
                          : connectivity.hopCounts[node.id] ?? "—"
                        : "Run analysis"}
                    </td>
                    <td>
                      <button
                        className="secondary-button"
                        type="button"
                        disabled={working}
                        onClick={() => {
                          setMacAddress(node.macAddress);
                          setRoute(null);
                        }}
                      >
                        Select MAC
                      </button>
                    </td>
                  </tr>
                ))}
                {!loading && topology.nodes.length === 0 && (
                  <tr>
                    <td colSpan={6}>No mesh nodes available.</td>
                  </tr>
                )}
                            </tbody>
            </table>
          </div>
        </section>

        <TelemetryHistoryPanel nodes={topology.nodes} />
      </section>
    </main>
  );
}

// Code attributes

/*
Title: How to Draw a Circle Using SVG Tag in HTML?
Author(s): GeeksforGeeks
Date: 2025
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/html/how-to-draw-a-circle-using-svg-tag-in-html/
*/