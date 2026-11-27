import { useState } from "react";

import api from "../api/client";

function getValue(reading) {
  return (
    reading.floatValue ??
    reading.integerValue ??
    (reading.booleanValue === null
      ? null
      : reading.booleanValue)
  );
}

export default function TelemetryHistoryPanel({ nodes }) {
  const [sensorId, setSensorId] = useState("");
  const [fromTime, setFromTime] = useState("00:00:00");
const [toTime, setToTime] = useState("23:59:59");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [readings, setReadings] = useState([]);
  const [working, setWorking] = useState(false);
  const [searched, setSearched] = useState(false);
  const [error, setError] = useState("");

  const sensorNodes = nodes.filter(
    (node) => node.sensorId
  );

  function clearResults() {
    setReadings([]);
    setSearched(false);
    setError("");
  }

  async function handleSearch(event) {
    event.preventDefault();
    clearResults();

    const fromUtc = new Date(`${fromDate}T${fromTime}`);
const toUtc = new Date(`${toDate}T${toTime}`);

    if (
      !Number.isFinite(fromUtc.getTime()) ||
      !Number.isFinite(toUtc.getTime())
    ) {
      setError("Select valid start and end dates.");
      return;
    }

    if (fromUtc > toUtc) {
      setError(
        "The start timestamp must not exceed the end timestamp."
      );
      return;
    }

    try {
      setWorking(true);

      const response = await api.get(
        `/telemetry/sensor/${sensorId}/range`,
        {
          params: {
            fromUtc: fromUtc.toISOString(),
            toUtc: toUtc.toISOString()
          }
        }
      );

      setReadings(response.data);
      setSearched(true);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setWorking(false);
    }
  }

  return (
    <section className="panel topology-panel">
      <div className="panel-heading">
        <div>
          <span className="section-label">
            Historical telemetry
          </span>
          <h2>Search readings by date range</h2>
        </div>

        {searched && (
          <span className="issue-count">
            {readings.length} readings
          </span>
        )}
      </div>

      <p className="topology-note">
        Select a sensor and a time window to review its
        recorded readings. Dates use your local time;
        the search includes both boundaries.
      </p>

      <form
        className="history-range-form"
        onSubmit={handleSearch}
      >
        <label className="form-field">
          <span>Sensor</span>
          <select
            value={sensorId}
            onChange={(event) => {
              setSensorId(event.target.value);
              clearResults();
            }}
            required
            disabled={working}
          >
            <option value="">Select a sensor</option>

            {sensorNodes.map((node) => (
              <option
                key={node.id}
                value={node.sensorId}
              >
                {node.name}
              </option>
            ))}
          </select>
        </label>

       <div className="history-date-group">
  <label className="form-field">
    <span>From date</span>
    <input
      type="date"
      value={fromDate}
      onChange={(event) => {
        setFromDate(event.target.value);
        clearResults();
      }}
      required
      disabled={working}
    />
  </label>

  <label className="form-field">
    <span>From time — local</span>
    <input
      type="time"
      step="1"
      value={fromTime}
      onChange={(event) => {
        setFromTime(event.target.value);
        clearResults();
      }}
      required
      disabled={working}
    />
  </label>
</div>

<div className="history-date-group">
  <label className="form-field">
    <span>To date</span>
    <input
      type="date"
      value={toDate}
      onChange={(event) => {
        setToDate(event.target.value);
        clearResults();
      }}
      required
      disabled={working}
    />
  </label>

  <label className="form-field">
    <span>To time — local</span>
    <input
      type="time"
      step="1"
      value={toTime}
      onChange={(event) => {
        setToTime(event.target.value);
        clearResults();
      }}
      required
      disabled={working}
    />
  </label>
</div>

        <button
          className="primary-button"
          type="submit"
          disabled={working || !sensorId}
        >
          {working ? "Searching…" : "Search history"}
        </button>
      </form>

      {error && (
        <div
          className="dashboard-error"
          role="alert"
        >
          {error}
        </div>
      )}

      {searched && (
        <div className="sensor-table-wrapper">
          <table className="sensor-table">
            <thead>
              <tr>
                <th>Recorded — local time</th>
                <th>Sequence</th>
                <th>Value</th>
                <th>Unit</th>
                <th>Payload type</th>
              </tr>
            </thead>

            <tbody>
              {readings.map((reading) => (
                <tr key={reading.id}>
                  <td>
                    {new Date(
                      reading.recordedAtUtc
                    ).toLocaleString()}
                  </td>
                  <td>{reading.sequenceNumber}</td>
                  <td>
                    {String(getValue(reading) ?? "—")}
                  </td>
                  <td>{reading.unit}</td>
                  <td>{reading.dataType}</td>
                </tr>
              ))}

              {readings.length === 0 && (
                <tr>
                  <td colSpan={5}>
                    No readings were recorded in this range.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}