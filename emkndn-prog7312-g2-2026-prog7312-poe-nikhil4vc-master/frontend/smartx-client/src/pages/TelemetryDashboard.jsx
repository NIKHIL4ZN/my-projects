import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  Cpu,
  Database,
  MapPin,
  Plus,
  Paperclip,
  Send,
  Radio,
  RefreshCw,
  ShieldAlert,
  Wifi
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useMemo,
  useState
} from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";

import api from "../api/client";
import SensorRegistrationModal from "../components/SensorRegistrationModal";
import TelemetrySimulatorModal from "../components/TelemetrySimulatorModal";
import SensorAttachmentsModal from "../components/SensorAttachmentsModal";
import useDashboard from "../context/useDashboard";

function getReadingValue(reading) {
  if (reading.floatValue !== null) {
    return reading.floatValue;
  }

  if (reading.integerValue !== null) {
    return reading.integerValue;
  }

  if (reading.booleanValue !== null) {
    return reading.booleanValue ? 1 : 0;
  }

  return null;
}

function formatTime(value) {
  return new Date(value).toLocaleTimeString(
    [],
    {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit"
    }
  );
}

export default function TelemetryDashboard() {
  const [sensors, setSensors] = useState([]);
  const [investigations, setInvestigations] =
    useState([]);
  const {
    selectedSensorId,
    setSelectedSensorId
  } = useDashboard();
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [historyLoading, setHistoryLoading] =
    useState(false);
  const [error, setError] = useState("");
  const [searchParams] = useSearchParams();
const followUpSensorId =
  searchParams.get("followUpSensor");

  const [
    registrationOpen,
    setRegistrationOpen
  ] = useState(false);

  const [
    simulatorOpen,
    setSimulatorOpen
  ] = useState(false);

  const [
    attachmentsOpen,
    setAttachmentsOpen
  ] = useState(false);

  const [
    historyRefreshKey,
    setHistoryRefreshKey
  ] = useState(0);

const loadDashboard = useCallback(
  async () => {
    try {
      setError("");

      const [
        sensorResponse,
        investigationResponse
      ] = await Promise.all([
        api.get("/sensors"),
        api.get("/investigations")
      ]);

      setSensors(sensorResponse.data);
      setInvestigations(investigationResponse.data);

      setSelectedSensorId((currentId) => {
        if (
          followUpSensorId &&
          sensorResponse.data.some(
            (sensor) => sensor.id === followUpSensorId
          )
        ) {
          return followUpSensorId;
        }

        return sensorResponse.data.some(
          (sensor) => sensor.id === currentId
        )
          ? currentId
          : sensorResponse.data[0]?.id || "";
      });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  },
  [setSelectedSensorId, followUpSensorId]
);

  async function handleTelemetryCreated() {
    setSimulatorOpen(false);

    setHistoryRefreshKey(
      (current) => current + 1
    );

    await loadDashboard();
  }

  async function handleSensorCreated(sensor) {
    setRegistrationOpen(false);
    setSelectedSensorId(sensor.id);

    await loadDashboard();
  }

  useEffect(() => {
    const timeoutId = window.setTimeout(
      () => {
        loadDashboard();
      },
      0
    );

    return () =>
      window.clearTimeout(timeoutId);
  }, [loadDashboard]);

  useEffect(() => {
    if (!selectedSensorId) {
      return;
    }

    async function loadHistory() {
      try {
        setHistoryLoading(true);
        setError("");

        const response = await api.get(
          `/telemetry/sensor/${selectedSensorId}`,
          {
            params: {
              limit: 100
            }
          }
        );

        setHistory(response.data);
      } catch (requestError) {
        setError(requestError.message);
      } finally {
        setHistoryLoading(false);
      }
    }

    loadHistory();
  }, [
    selectedSensorId,
    historyRefreshKey
  ]);

  const selectedSensor = sensors.find(
    (sensor) => sensor.id === selectedSensorId
  );

  const chartData = useMemo(
    () =>
      [...history]
        .reverse()
        .map((reading) => ({
          sequence: reading.sequenceNumber,
          time: formatTime(reading.recordedAtUtc),
          value: getReadingValue(reading),
          unit: reading.unit
        })),
    [history]
  );

  const criticalCount = investigations.filter(
    (item) => item.severity === "Critical"
  ).length;

  const onlineCount = sensors.filter(
    (sensor) => sensor.status === "Online"
  ).length;

  const latestReading = history[0];

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
          <span className="nav-label">Part 1</span>

          <Link to="/dashboard" className="nav-item">
  <Cpu size={18} />
  Overview
</Link>

          <button className="nav-item nav-item--active">
            <Activity size={18} />
            Telemetry
          </button>

          <Link
            to="/commands"
            className="nav-item"
          >
            <Radio size={18} />
            Command Stream
            <small>Part 2</small>
          </Link>

          <Link
  to="/topology"
  className="nav-item"
>
  <Wifi size={18} />
  Mesh Routing
  <small>PoE</small>
</Link>
        </nav>

        <div className="sidebar-status">
          <span className="status-dot" />

          <div>
            <strong>API connected</strong>
            <small>.NET gateway online</small>
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

            <h1>Telemetry Gateway</h1>

            <p>
              Monitor registered sensors and investigate
              anomalous readings.
            </p>
          </div>

          <div className="header-actions">
            <button
              className="refresh-button"
              onClick={loadDashboard}
              type="button"
            >
              <RefreshCw size={17} />
              Refresh data
            </button>

            <button
              className="primary-button"
              onClick={() =>
                setRegistrationOpen(true)
              }
              type="button"
            >
              <Plus size={17} />
              Register sensor
            </button>
          </div>
        </header>

        {error && (
          <div className="dashboard-error">
            <AlertTriangle size={18} />
            {error}
          </div>
        )}

        <section className="metric-grid">
          <article className="metric-card">
            <span className="metric-icon">
              <Cpu size={20} />
            </span>

            <div>
              <small>Registered sensors</small>
              <strong>
                {loading ? "—" : sensors.length}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon">
              <Radio size={20} />
            </span>

            <div>
              <small>Online devices</small>
              <strong>
                {loading ? "—" : onlineCount}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon metric-icon--danger">
              <ShieldAlert size={20} />
            </span>

            <div>
              <small>Critical investigations</small>
              <strong>
                {loading ? "—" : criticalCount}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon">
              <Database size={20} />
            </span>

            <div>
              <small>Selected history</small>
              <strong>
                {historyLoading ? "—" : history.length}
              </strong>
            </div>
          </article>
        </section>

        <section className="dashboard-main-grid">
          <article className="panel telemetry-panel">
            <div className="panel-heading">
              <div>
                <span className="section-label">
                  Live telemetry
                </span>

                <h2>
                  {selectedSensor?.name ||
                    "Select a sensor"}
                </h2>

                {selectedSensor && (
                  <p>
                    <MapPin size={14} />
                    {selectedSensor.deploymentLocation}
                  </p>
                )}
              </div>

              <div className="telemetry-controls">
                <select
                  className="sensor-select"
                  value={selectedSensorId}
                  onChange={(event) =>
                    setSelectedSensorId(
                      event.target.value
                    )
                  }
                >
                  {sensors.map((sensor) => (
                    <option
                      value={sensor.id}
                      key={sensor.id}
                    >
                      {sensor.name}
                    </option>
                  ))}
                </select>

                <button
                  className="secondary-button"
                  onClick={() =>
                    setAttachmentsOpen(true)
                  }
                  type="button"
                  disabled={!selectedSensor}
                >
                  <Paperclip size={16} />
                  Files
                </button>

                <button
                  className="secondary-button"
                  onClick={() =>
                    setSimulatorOpen(true)
                  }
                  type="button"
                  disabled={!selectedSensor}
                >
                  <Send size={16} />
                  Simulate
                </button>
              </div>
            </div>

            <div className="latest-reading">
              <div>
                <small>Latest value</small>

                <strong>
                  {latestReading
                    ? `${getReadingValue(
                      latestReading
                    )} ${latestReading.unit}`
                    : "No telemetry"}
                </strong>
              </div>

              {latestReading && (
                <span>
                  Sequence #
                  {latestReading.sequenceNumber}
                </span>
              )}
            </div>

            <div className="chart-container">
              {chartData.length > 0 ? (
                <ResponsiveContainer
                  width="100%"
                  height="100%"
                >
                  <LineChart data={chartData}>
                    <CartesianGrid
                      stroke="rgba(255,255,255,0.06)"
                      vertical={false}
                    />

                    <XAxis
                      dataKey="sequence"
                      stroke="#65788b"
                      tickLine={false}
                      axisLine={false}
                    />

                    <YAxis
                      stroke="#65788b"
                      tickLine={false}
                      axisLine={false}
                      width={38}
                    />

                    <Tooltip
                      contentStyle={{
                        background: "#101e2a",
                        border:
                          "1px solid rgba(255,255,255,.1)",
                        borderRadius: "10px"
                      }}
                      labelFormatter={(value) =>
                        `Sequence ${value}`
                      }
                    />

                    <Line
                      type="monotone"
                      dataKey="value"
                      stroke="#42d8b3"
                      strokeWidth={3}
                      dot={{
                        fill: "#42d8b3",
                        strokeWidth: 0,
                        r: 3
                      }}
                      activeDot={{
                        r: 6,
                        fill: "#071019",
                        stroke: "#42d8b3",
                        strokeWidth: 3
                      }}
                    />
                  </LineChart>
                </ResponsiveContainer>
              ) : (
                <div className="empty-state">
                  <Activity size={28} />
                  No telemetry available
                </div>
              )}
            </div>
          </article>

          <article className="panel investigation-panel">
            <div className="panel-heading">
              <div>
                <span className="section-label">
                  Guided investigation
                </span>

                <h2>Action centre</h2>
              </div>

              <span className="issue-count">
                {investigations.length} issues
              </span>
            </div>

            <div className="investigation-list">
              {investigations.length === 0 ? (
                <div className="empty-state">
                  <ShieldAlert size={28} />
                  No anomalies detected
                </div>
              ) : (
                investigations.map(
                  (investigation) => (
                    <details
                      className="investigation-card"
                      key={
                        investigation.investigationId
                      }
                    >
                      <summary>
                        <span
                          className={`severity-dot severity-dot--${investigation.severity.toLowerCase()}`}
                        />

                        <div>
                          <small>
                            {investigation.severity}
                          </small>

                          <strong>
                            {investigation.title}
                          </strong>

                          <span>
                            {investigation.sensorName}
                          </span>
                        </div>
                      </summary>

                      <div className="investigation-body">
                        <p>
                          {investigation.explanation}
                        </p>

                        <div className="confidence">
                          Confidence:{" "}
                          {investigation.confidencePercent.toFixed(
                            0
                          )}
                          %
                        </div>

                        <ul className="evidence-list">
                          {investigation.evidence.map(
                            (evidence) => (
                              <li key={evidence}>
                                {evidence}
                              </li>
                            )
                          )}
                        </ul>

                        <ol className="step-list">
                          {investigation.steps.map(
                            (step) => (
                              <li key={step.order}>
                                <span>
                                  {step.order}
                                </span>

                                <div>
                                  <strong>
                                    {step.title}
                                  </strong>

                                  <p>
                                    {step.guidance}
                                  </p>
                                </div>
                              </li>
                            )
                          )}
                        </ol>
                      </div>
                    </details>
                  )
                )
              )}
            </div>
          </article>
        </section>

        <section className="panel sensor-panel">
          <div className="panel-heading">
            <div>
              <span className="section-label">
                Device registry
              </span>

              <h2>Registered sensors</h2>
            </div>
          </div>

          <div className="sensor-table-wrapper">
            <table className="sensor-table">
              <thead>
                <tr>
                  <th>Sensor</th>
                  <th>MAC address</th>
                  <th>Location</th>
                  <th>Category</th>
                  <th>Payload</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                {sensors.map((sensor) => (
                  <tr
                    key={sensor.id}
                    onClick={() =>
                      setSelectedSensorId(sensor.id)
                    }
                  >
                    <td>
                      <strong>{sensor.name}</strong>
                    </td>
                    <td>
                      <code>{sensor.macAddress}</code>
                    </td>
                    <td>
                      {sensor.deploymentLocation}
                    </td>
                    <td>{sensor.category}</td>
                    <td>{sensor.dataType}</td>
                    <td>
                      <span className="online-badge">
                        {sensor.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </section>
      {registrationOpen && (
        <SensorRegistrationModal
          onClose={() =>
            setRegistrationOpen(false)
          }
          onCreated={handleSensorCreated}
        />
      )}

      {simulatorOpen && selectedSensor && (
        <TelemetrySimulatorModal
          sensor={selectedSensor}
          nextSequenceNumber={
            latestReading
              ? latestReading.sequenceNumber + 1
              : 0
          }
          onClose={() =>
            setSimulatorOpen(false)
          }
          onCreated={handleTelemetryCreated}
        />
      )}

      {attachmentsOpen && selectedSensor && (
        <SensorAttachmentsModal
          sensor={selectedSensor}
          onClose={() =>
            setAttachmentsOpen(false)
          }
        />
      )}
    </main>
  );
}

// Code attributes

/*
Title: useSearchParams
Author(s): React Router Contributors
Date: n.d.
Version: React Router 8.4.0
Type: Documentation
Availability: https://reactrouter.com/api/hooks/useSearchParams
*/