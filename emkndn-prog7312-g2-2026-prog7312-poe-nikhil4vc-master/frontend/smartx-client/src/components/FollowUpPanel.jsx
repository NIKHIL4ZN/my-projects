import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

const storageKey = "smartx.followUps";

function loadTasks() {
  try {
    const stored = JSON.parse(
      localStorage.getItem(storageKey) || "[]"
    );

    if (!Array.isArray(stored)) return [];

    return stored.filter(
      (task) =>
        typeof task.id === "string" &&
        typeof task.sensorId === "string" &&
        typeof task.sensorName === "string" &&
        typeof task.note === "string" &&
        typeof task.completed === "boolean" &&
        Number.isFinite(Date.parse(task.dueAt))
    );
  } catch {
    return [];
  }
}

export default function FollowUpPanel({ sensors }) {
  const [tasks, setTasks] = useState(loadTasks);
  const [sensorId, setSensorId] = useState("");
  const [note, setNote] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [dueTime, setDueTime] = useState("09:00");
  const [error, setError] = useState("");
  const [storageError, setStorageError] = useState("");
  const [now, setNow] = useState(() => Date.now());


  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setNow(Date.now());
    }, 60000);

    return () => window.clearInterval(intervalId);
  }, []);

  function saveTasks(updatedTasks) {
  setTasks(updatedTasks);

  try {
    localStorage.setItem(
      storageKey,
      JSON.stringify(updatedTasks)
    );
    setStorageError("");
  } catch {
    setStorageError(
      "Follow-ups could not be saved in this browser."
    );
  }
}

function handleAdd(event) {
  event.preventDefault();
  setError("");

  const sensor = sensors.find(
    (item) => item.id === sensorId
  );

  const dueAt = new Date(`${dueDate}T${dueTime}`);

  if (
    !sensor ||
    !note.trim() ||
    !Number.isFinite(dueAt.getTime())
  ) {
    setError(
      "Select a sensor, enter a review note, and choose a valid date."
    );
    return;
  }

  saveTasks([
    ...tasks,
    {
      id: crypto.randomUUID(),
      sensorId: sensor.id,
      sensorName: sensor.name,
      note: note.trim(),
      dueAt: dueAt.toISOString(),
      completed: false
    }
  ]);

  setNote("");
  setNow(Date.now());
}

function toggleCompleted(taskId) {
  saveTasks(
    tasks.map((task) =>
      task.id === taskId
        ? { ...task, completed: !task.completed }
        : task
    )
  );
}

function removeTask(taskId) {
  saveTasks(
    tasks.filter((task) => task.id !== taskId)
  );
}

  const pendingTasks = tasks.filter(
    (task) => !task.completed
  );

  const dueCount = pendingTasks.filter(
    (task) => Date.parse(task.dueAt) <= now
  ).length;

  const displayedTasks = [...tasks].sort(
    (first, second) =>
      Number(first.completed) - Number(second.completed) ||
      Date.parse(first.dueAt) - Date.parse(second.dueAt)
  );

  return (
    <section className="panel follow-up-panel">
      <div className="panel-heading">
        <div>
          <span className="section-label">
            Return and review
          </span>
          <h2>Sensor follow-ups</h2>
        </div>

        <span className="issue-count">
          {dueCount} due · {pendingTasks.length} pending
        </span>
      </div>

      <p className="topology-note">
        Schedule a review after an investigation or command.
        Return to this dashboard to check due tasks.
        Follow-ups are saved in this browser.
      </p>

      <form
        className="follow-up-form"
        onSubmit={handleAdd}
      >
        <label className="form-field">
          <span>Sensor to review</span>
          <select
            value={sensorId}
            onChange={(event) =>
              setSensorId(event.target.value)
            }
            required
          >
            <option value="">Select sensor</option>
            {sensors.map((sensor) => (
              <option key={sensor.id} value={sensor.id}>
                {sensor.name}
              </option>
            ))}
          </select>
        </label>

        <label className="form-field">
          <span>Review note</span>
          <input
            value={note}
            onChange={(event) =>
              setNote(event.target.value)
            }
            placeholder="Check readings after calibration"
            maxLength={250}
            required
          />
        </label>

        <label className="form-field">
          <span>Review date — local</span>
          <input
            type="date"
            value={dueDate}
            onChange={(event) =>
              setDueDate(event.target.value)
            }
            required
          />
        </label>

        <label className="form-field">
          <span>Review time — local</span>
          <input
            type="time"
            value={dueTime}
            onChange={(event) =>
              setDueTime(event.target.value)
            }
            required
          />
        </label>

        <button
          className="primary-button"
          type="submit"
          disabled={sensors.length === 0}
        >
          Add follow-up
        </button>
      </form>

      {(error || storageError) && (
        <div className="dashboard-error" role="alert">
          {error || storageError}
        </div>
      )}

      <div className="follow-up-list">
        {displayedTasks.map((task) => {
          const due =
            !task.completed &&
            Date.parse(task.dueAt) <= now;

          const sensorExists = sensors.some(
            (sensor) => sensor.id === task.sensorId
          );

          return (
            <article
              className="follow-up-card"
              key={task.id}
            >
              <div>
                <span className="section-label">
                  {task.completed
                    ? "Completed"
                    : due
                      ? "Due for review"
                      : "Upcoming"}
                </span>
                <h3>{task.sensorName}</h3>
                <p>{task.note}</p>
                <small>
                  Review:{" "}
                  {new Date(task.dueAt).toLocaleString()}
                </small>
              </div>

              <div className="follow-up-actions">
                {sensorExists && (
                  <Link
                    to={`/telemetry?followUpSensor=${encodeURIComponent(
                      task.sensorId
                    )}`}
                    className="secondary-button"
                  >
                    Review sensor
                  </Link>
                )}

                <button
                  className="secondary-button"
                  type="button"
                  onClick={() => toggleCompleted(task.id)}
                >
                  {task.completed ? "Reopen" : "Mark complete"}
                </button>

                <button
                  className="secondary-button"
                  type="button"
                  onClick={() => removeTask(task.id)}
                >
                  Remove
                </button>
              </div>
            </article>
          );
        })}

        {tasks.length === 0 && (
          <p className="topology-note">
            No follow-ups yet. Schedule your next sensor review.
          </p>
        )}
      </div>
    </section>
  );
}

// Code attributes

/*
Title: Crypto: randomUUID() method
Author(s): MDN contributors
Date: 2024
Version: Not applicable
Type: Documentation
Availability: https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID
*/

/*
Title: How to clean up setInterval in useEffect using react hooks
Author(s): Darruma
Date: 2019
Version: Not applicable
Type: Stack Overflow question
Availability: https://stackoverflow.com/questions/55763957/how-to-clean-up-setinterval-in-useeffect-using-react-hooks
*/

/*
Title: LocalStorage and JSON.stringify JSON.parse
Author(s): king_sw4
Date: 2014
Version: Not applicable
Type: Stack Overflow question
Availability: https://stackoverflow.com/questions/23728626/localstorage-and-json-stringify-json-parse
*/

