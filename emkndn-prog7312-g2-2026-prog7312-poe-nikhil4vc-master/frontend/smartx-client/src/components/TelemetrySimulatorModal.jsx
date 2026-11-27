import {
  LoaderCircle,
  Send,
  X
} from "lucide-react";
import { useState } from "react";

import api from "../api/client";

function getDefaultUnit(sensor) {
  if (sensor.dataType === "Boolean") {
    return "state";
  }

  if (
    sensor.category === "PowerConsumption"
  ) {
    return "W";
  }

  return "%";
}

export default function TelemetrySimulatorModal({
  sensor,
  nextSequenceNumber,
  onClose,
  onCreated
}) {

  const [unit, setUnit] = useState(
    getDefaultUnit(sensor)
  );

  const [submitting, setSubmitting] =
    useState(false);

  const [value, setValue] = useState(
    () =>
      sensor.dataType === "Boolean"
        ? "true"
        : ""
  );

  const [error, setError] = useState("");

  function parseValue() {
    if (sensor.dataType === "Float") {
      return Number.parseFloat(value);
    }

    if (sensor.dataType === "Integer") {
      return Number.parseInt(value, 10);
    }

    return value === "true";
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const parsedValue = parseValue();

    if (
      sensor.dataType !== "Boolean" &&
      !Number.isFinite(parsedValue)
    ) {
      setError(
        "Enter a valid numeric telemetry value."
      );
      return;
    }

    try {
      setSubmitting(true);
      setError("");

      const endpoint =
        sensor.dataType.toLowerCase();

      const response = await api.post(
        `/telemetry/${endpoint}`,
        {
          sensorId: sensor.id,
          value: parsedValue,
          unit,
          sequenceNumber:
            nextSequenceNumber
        }
      );

      await onCreated(response.data);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <section
        className="registration-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="simulator-title"
      >
        <header className="modal-header">
          <div className="modal-title">
            <span className="modal-icon">
              <Send size={20} />
            </span>

            <div>
              <span className="section-label">
                Telemetry simulator
              </span>

              <h2 id="simulator-title">
                Publish a sensor packet
              </h2>
            </div>
          </div>

          <button
            className="icon-button"
            onClick={onClose}
            type="button"
            aria-label="Close simulator"
          >
            <X size={19} />
          </button>
        </header>

        <p className="modal-description">
          Simulate an incoming packet from{" "}
          <strong>{sensor.name}</strong>.
          The API will enforce its{" "}
          {sensor.dataType} payload type.
        </p>

        {error && (
          <div className="form-error">
            {error}
          </div>
        )}

        <form
          className="registration-form"
          onSubmit={handleSubmit}
        >
          <div className="sensor-context form-field--wide">
            <div>
              <small>MAC address</small>
              <strong>{sensor.macAddress}</strong>
            </div>

            <div>
              <small>Payload</small>
              <strong>{sensor.dataType}</strong>
            </div>

            <div>
              <small>Sequence</small>
              <strong>
                #{nextSequenceNumber}
              </strong>
            </div>
          </div>

          <label className="form-field">
            <span>Telemetry value</span>

            {sensor.dataType === "Boolean" ? (
              <select
                value={value}
                onChange={(event) =>
                  setValue(event.target.value)
                }
              >
                <option value="true">
                  True / Active
                </option>

                <option value="false">
                  False / Inactive
                </option>
              </select>
            ) : (
              <input
                value={value}
                onChange={(event) =>
                  setValue(event.target.value)
                }
                type="number"
                step={
                  sensor.dataType === "Float"
                    ? "any"
                    : "1"
                }
                min={
                  sensor.dataType === "Integer"
                    ? "0"
                    : undefined
                }
                placeholder={
                  sensor.dataType === "Float"
                    ? "e.g. 42.5"
                    : "e.g. 850"
                }
                required
              />
            )}
          </label>

          <label className="form-field">
            <span>Measurement unit</span>

            <input
              value={unit}
              onChange={(event) =>
                setUnit(event.target.value)
              }
              maxLength={20}
              placeholder="%, W, °C or state"
              required
            />
          </label>

          <div className="form-guidance form-field--wide">
            <strong>
              Generic packet pipeline
            </strong>

            <p>
              This request will be processed as{" "}
              TelemetryPacket&lt;
              {sensor.dataType === "Float"
                ? "double"
                : sensor.dataType.toLowerCase()}
              &gt; without converting the payload
              to object.
            </p>
          </div>

          <footer className="modal-actions form-field--wide">
            <button
              className="secondary-button"
              onClick={onClose}
              type="button"
              disabled={submitting}
            >
              Cancel
            </button>

            <button
              className="primary-button"
              type="submit"
              disabled={submitting}
            >
              {submitting ? (
                <>
                  <LoaderCircle
                    className="spin"
                    size={17}
                  />
                  Publishing
                </>
              ) : (
                <>
                  <Send size={17} />
                  Publish packet
                </>
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
