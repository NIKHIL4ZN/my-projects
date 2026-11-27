import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  Cpu,
  ListPlus,
  Play,
  Radio,
  RefreshCw,
  RotateCcw,
  Send,
  Sparkles,
  Wifi
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";
import { Link } from "react-router-dom";

import api from "../api/client";
import useDashboard from "../context/useDashboard";

const commandTypes = [
  { value: 1, label: "Restart" },
  { value: 2, label: "Calibrate" },
  {
    value: 3,
    label: "Change sample interval"
  },
  { value: 4, label: "Activate" },
  { value: 5, label: "Deactivate" },
  { value: 6, label: "Request status" }
];

const priorities = [
  { value: 1, label: "Normal" },
  { value: 2, label: "High" },
  { value: 3, label: "Critical" }
];

const commandTypeValues = {
  Restart: 1,
  Calibrate: 2,
  ChangeSampleInterval: 3,
  Activate: 4,
  Deactivate: 5,
  RequestStatus: 6
};

const priorityValues = {
  Normal: 1,
  High: 2,
  Critical: 3
};

function formatDate(value) {
  if (!value) {
    return "Not processed";
  }

  return new Date(value).toLocaleString();
}

export default function CommandStreamDashboard() {
  const {
    selectedSensorId,
    setSelectedSensorId
  } = useDashboard();

  const [sensors, setSensors] = useState([]);
  const [commands, setCommands] = useState([]);
  const [recommendations, setRecommendations] =
    useState([]);
  const [summary, setSummary] = useState({
    criticalCommands: 0,
    normalCommands: 0,
    completedCommands: 0,
    uniqueDevices: 0,
    totalTrackedCommands: 0
  });
  const [type, setType] = useState(6);
  const [priority, setPriority] = useState(1);
  const [payload, setPayload] = useState(
    "Return current device status"
  );
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [deviceSearch, setDeviceSearch] =
    useState("");

  const [timeline, setTimeline] =
    useState([]);

  const [historyView, setHistoryView] =
    useState("history");

  const loadCommandData = useCallback(
    async () => {
      try {
        setError("");

        const [
          sensorResponse,
          commandResponse,
          timelineResponse,
          summaryResponse,
          recommendationResponse
        ] = await Promise.all([
          api.get("/sensors"),
          api.get("/commands"),
          api.get("/commands/timeline"),
          api.get("/commands/summary"),
          api.get("/recommendations")
        ]);

        setSensors(sensorResponse.data);
        setCommands(commandResponse.data);
        setTimeline(timelineResponse.data);
        setSummary(summaryResponse.data);
        setRecommendations(
          recommendationResponse.data
        );

        const selectedStillExists =
          sensorResponse.data.some(
            (sensor) =>
              sensor.id === selectedSensorId
          );

        if (
          !selectedStillExists &&
          sensorResponse.data.length > 0
        ) {
          setSelectedSensorId(
            sensorResponse.data[0].id
          );
        }
      } catch (requestError) {
        setError(requestError.message);
      } finally {
        setLoading(false);
      }
    },
    [
      selectedSensorId,
      setSelectedSensorId
    ]
  );

  useEffect(() => {
    const timeoutId = window.setTimeout(
      () => {
        loadCommandData();
      },
      0
    );

    return () =>
      window.clearTimeout(timeoutId);
  }, [loadCommandData]);

  async function executeAction(action) {
    try {
      setWorking(true);
      setError("");
      setMessage("");

      const response = await action();

      if (response?.data?.message) {
        setMessage(response.data.message);
      }

      await loadCommandData();

      return response;
    } catch (requestError) {
      setError(requestError.message);
      return null;
    } finally {
      setWorking(false);
    }
  }

  async function handleQueueCommand(event) {
    event.preventDefault();

    const response = await executeAction(
      () =>
        api.post("/commands", {
          sensorId: selectedSensorId,
          type: Number(type),
          priority: Number(priority),
          payload
        })
    );

    if (response) {
      setMessage(
        "The command was added to the queue."
      );
    }
  }

  async function handleProcessNext() {
    const response = await executeAction(
      () =>
        api.post("/commands/process-next")
    );

    if (response) {
      setMessage(
        response.data
          ? "The next command was processed."
          : "There are no queued commands."
      );
    }
  }

  async function handleUndo() {
    const response = await executeAction(
      () => api.post("/commands/undo")
    );

    if (response) {
      setMessage(
        response.data
          ? "The last completed command was undone."
          : "There are no completed commands to undo."
      );
    }
  }

  async function handleAnalyse(event) {
    event.preventDefault();

    if (!query.trim()) {
      return;
    }

    const response = await executeAction(
      () =>
        api.post(
          "/recommendations/analyse",
          {
            query: query.trim()
          }
        )
    );

    if (response) {
      setRecommendations(response.data);
      setQuery("");
      setMessage(
        "Operator query analysed successfully."
      );
    }
  }

  async function handleQueueRecommendation(
    recommendation
  ) {
    const commandType =
      commandTypeValues[
      recommendation.recommendedCommandType
      ];

    const commandPriority =
      priorityValues[recommendation.priority];

    if (!commandType || !commandPriority) {
      setError(
        "The recommendation contains an unsupported command."
      );
      return;
    }

    const response = await executeAction(
      () =>
        api.post("/commands", {
          sensorId: recommendation.sensorId,
          type: commandType,
          priority: commandPriority,
          payload:
            recommendation.suggestedPayload
        })
    );

    if (response) {
      setSelectedSensorId(
        recommendation.sensorId
      );

      setMessage(
        `${recommendation.recommendedCommandType} ` +
        `was queued for ${recommendation.sensorName}.`
      );
    }
  }

  const selectedSensor = sensors.find(
    (sensor) =>
      sensor.id === selectedSensorId
  );

  const filteredSensors = sensors.filter(
    (sensor) => {
      const search =
        deviceSearch.trim().toLowerCase();

      if (!search) {
        return true;
      }

      return (
        sensor.name
          .toLowerCase()
          .includes(search) ||
        sensor.macAddress
          .toLowerCase()
          .includes(search) ||
        sensor.deploymentLocation
          .toLowerCase()
          .includes(search)
      );
    }
  );

  const displayedCommands =
    historyView === "timeline"
      ? timeline
      : commands;

  return (
    <main className="dashboard-page">
      <aside className="dashboard-sidebar">
        <Link
          to="/"
          className="dashboard-brand"
        >
          <span className="brand-mark">
            <Cpu size={21} />
          </span>

          <span>
            <strong>Smart-X</strong>
            <small>Engineering Console</small>
          </span>
        </Link>

        <nav className="dashboard-nav">
          <span className="nav-label">
            Modules
          </span>

          <Link to="/dashboard" className="nav-item">
  <Cpu size={18} />
  Overview
</Link>

          <Link
            to="/telemetry"
            className="nav-item"
          >
            <Activity size={18} />
            Telemetry
            <small>Part 1</small>
          </Link>

          <Link
            to="/commands"
            className="nav-item nav-item--active"
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
            <small>Command gateway online</small>
          </div>
        </div>
      </aside>

      <section className="dashboard-content">
        <header className="dashboard-header">
          <div>
            <Link
              to="/"
              className="back-link"
            >
              <ArrowLeft size={15} />
              All modules
            </Link>

            <h1>Command Stream</h1>

            <p>
              Queue device commands, process priority
              actions, and review predictive suggestions.
            </p>
          </div>

          <div className="header-actions">
            <button
              className="refresh-button"
              onClick={loadCommandData}
              type="button"
              disabled={working}
            >
              <RefreshCw size={17} />
              Refresh data
            </button>

            <button
              className="secondary-button"
              onClick={handleUndo}
              type="button"
              disabled={working}
            >
              <RotateCcw size={17} />
              Undo last
            </button>

            <button
              className="primary-button"
              onClick={handleProcessNext}
              type="button"
              disabled={working}
            >
              <Play size={17} />
              Process next
            </button>
          </div>
        </header>

        {error && (
          <div className="dashboard-error">
            <AlertTriangle size={18} />
            {error}
          </div>
        )}

        {message && (
          <div className="dashboard-success">
            {message}
          </div>
        )}

        <section className="metric-grid">
          <article className="metric-card">
            <span className="metric-icon">
              <Radio size={20} />
            </span>

            <div>
              <small>Priority queue</small>
              <strong>
                {loading
                  ? "—"
                  : summary.criticalCommands}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon">
              <Send size={20} />
            </span>

            <div>
              <small>Normal queue</small>
              <strong>
                {loading
                  ? "—"
                  : summary.normalCommands}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon">
              <RotateCcw size={20} />
            </span>

            <div>
              <small>Completed</small>
              <strong>
                {loading
                  ? "—"
                  : summary.completedCommands}
              </strong>
            </div>
          </article>

          <article className="metric-card">
            <span className="metric-icon">
              <Cpu size={20} />
            </span>

            <div>
              <small>Unique devices</small>
              <strong>
                {loading
                  ? "—"
                  : summary.uniqueDevices}
              </strong>
            </div>
          </article>
        </section>

        <section className="dashboard-main-grid">
          <article className="panel command-form-panel">
            <div className="panel-heading">
              <div>
                <span className="section-label">
                  Queue management
                </span>

                <h2>Send device command</h2>
              </div>
            </div>

            <form
              className="registration-form"
              onSubmit={handleQueueCommand}
            >
              <label className="form-field form-field--wide">
                <span>Find device</span>

                <input
                  value={deviceSearch}
                  onChange={(event) =>
                    setDeviceSearch(event.target.value)
                  }
                  placeholder="Search by name, MAC address or location"
                />
              </label>

              <label className="form-field form-field--wide">
                <span>Target sensor</span>

                <select
                  value={selectedSensorId}
                  onChange={(event) =>
                    setSelectedSensorId(
                      event.target.value
                    )
                  }
                  required
                >
                  {filteredSensors.map((sensor) => (
                    <option
                      value={sensor.id}
                      key={sensor.id}
                    >
                      {sensor.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="form-field">
                <span>Command type</span>

                <select
                  value={type}
                  onChange={(event) =>
                    setType(event.target.value)
                  }
                >
                  {commandTypes.map((command) => (
                    <option
                      value={command.value}
                      key={command.value}
                    >
                      {command.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="form-field">
                <span>Priority</span>

                <select
                  value={priority}
                  onChange={(event) =>
                    setPriority(event.target.value)
                  }
                >
                  {priorities.map((item) => (
                    <option
                      value={item.value}
                      key={item.value}
                    >
                      {item.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="form-field form-field--wide">
                <span>Command payload</span>

                <textarea
                  value={payload}
                  onChange={(event) =>
                    setPayload(event.target.value)
                  }
                  maxLength={500}
                  rows={4}
                  required
                />
              </label>

              <button
                className="primary-button form-field--wide"
                type="submit"
                disabled={
                  working ||
                  !selectedSensor
                }
              >
                <Send size={17} />
                Queue command
              </button>
            </form>
          </article>

          <article className="panel recommendation-panel">
            <div className="panel-heading">
              <div>
                <span className="section-label">
                  Predictive actions
                </span>

                <h2>Recommendation engine</h2>
              </div>

              <span className="issue-count">
                {recommendations.length} suggestions
              </span>
            </div>

            <form
              className="query-form"
              onSubmit={handleAnalyse}
            >
              <input
                value={query}
                onChange={(event) =>
                  setQuery(event.target.value)
                }
                placeholder="e.g. Investigate power spike"
              />

              <button
                className="secondary-button"
                type="submit"
                disabled={working}
              >
                <Sparkles size={16} />
                Analyse
              </button>
            </form>

            <div className="investigation-list">
              {recommendations
                .slice(0, 6)
                .map((recommendation) => (
                  <article
                    className="investigation-card"
                    key={
                      recommendation.recommendationId
                    }
                  >
                    <div className="recommendation-content">
                      <small>
                        {recommendation.priority}
                        {" · "}
                        {recommendation.confidencePercent.toFixed(
                          0
                        )}
                        % confidence
                      </small>

                      <strong>
                        {recommendation.title}
                      </strong>

                      <span>
                        {recommendation.sensorName}
                      </span>

                      <p>
                        {recommendation.explanation}
                      </p>
                      <button
                        className="recommendation-action"
                        onClick={() =>
                          handleQueueRecommendation(
                            recommendation
                          )
                        }
                        type="button"
                        disabled={working}
                      >
                        <ListPlus size={15} />
                        Queue{" "}
                        {recommendation.recommendedCommandType}
                      </button>
                    </div>
                  </article>
                ))}
            </div>
          </article>
        </section>

        <section className="panel sensor-panel command-history-panel">
          <div className="panel-heading">
            <div>
              <span className="section-label">
                Command dictionary and timeline
              </span>

              <h2>Command history</h2>
            </div>

            <div className="history-controls">
              <button
                className={
                  historyView === "history"
                    ? "history-toggle history-toggle--active"
                    : "history-toggle"
                }
                onClick={() =>
                  setHistoryView("history")
                }
                type="button"
              >
                Latest first
              </button>

              <button
                className={
                  historyView === "timeline"
                    ? "history-toggle history-toggle--active"
                    : "history-toggle"
                }
                onClick={() =>
                  setHistoryView("timeline")
                }
                type="button"
              >
                Timeline
              </button>
            </div>
          </div>

          <div className="sensor-table-wrapper">
            <table className="sensor-table">
              <thead>
                <tr>
                  <th>Device</th>
                  <th>Command</th>
                  <th>Priority</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Processed</th>
                </tr>
              </thead>

              <tbody>
                {displayedCommands.map(
                  (command) => (
                    <tr key={command.id}>
                      <td>
                        <strong>
                          {command.sensorName}
                        </strong>
                      </td>

                      <td>{command.type}</td>

                      <td>{command.priority}</td>

                      <td>
                        <span className="online-badge">
                          {command.status}
                        </span>
                      </td>

                      <td>
                        {formatDate(
                          command.createdAtUtc
                        )}
                      </td>

                      <td>
                        {formatDate(
                          command.processedAtUtc
                        )}
                      </td>
                    </tr>
                  )
                )}

                {!loading &&
                  displayedCommands.length === 0 && (
                    <tr>
                      <td colSpan={6}>
                        No commands have been queued.
                      </td>
                    </tr>
                  )}
              </tbody>
            </table>
          </div>
        </section>
      </section>
    </main>
  );
}