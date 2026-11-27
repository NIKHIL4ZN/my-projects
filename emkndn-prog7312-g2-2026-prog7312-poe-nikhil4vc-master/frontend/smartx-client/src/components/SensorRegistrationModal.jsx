import {
  Cpu,
  LoaderCircle,
  X
} from "lucide-react";
import { useState } from "react";

import api from "../api/client";

const initialForm = {
  name: "",
  macAddress: "",
  deploymentLocation: "",
  category: "Environmental",
  dataType: "Float"
};

export default function SensorRegistrationModal({
  onClose,
  onCreated
}) {
  const [form, setForm] = useState(initialForm);
  const [submitting, setSubmitting] =
    useState(false);
  const [error, setError] = useState("");

  function updateField(event) {
    const { name, value } = event.target;

    setForm((current) => ({
      ...current,
      [name]: value
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    try {
      setSubmitting(true);
      setError("");

      const response = await api.post(
        "/sensors",
        form
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
        aria-labelledby="registration-title"
      >
        <header className="modal-header">
          <div className="modal-title">
            <span className="modal-icon">
              <Cpu size={21} />
            </span>

            <div>
              <span className="section-label">
                Device registry
              </span>

              <h2 id="registration-title">
                Register a sensor
              </h2>
            </div>
          </div>

          <button
            className="icon-button"
            onClick={onClose}
            type="button"
            aria-label="Close registration form"
          >
            <X size={19} />
          </button>
        </header>

        <p className="modal-description">
          Attach an ESP32 device or simulated sensor to
          the Smart-X telemetry gateway.
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
          <label className="form-field form-field--wide">
            <span>Sensor name</span>

            <input
              name="name"
              value={form.name}
              onChange={updateField}
              minLength={2}
              maxLength={100}
              placeholder="e.g. Greenhouse Temperature A"
              required
            />
          </label>

          <label className="form-field">
            <span>MAC address</span>

            <input
              name="macAddress"
              value={form.macAddress}
              onChange={updateField}
              pattern="^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$"
              placeholder="AA:BB:CC:DD:EE:FF"
              title="Use the format AA:BB:CC:DD:EE:FF"
              required
            />
          </label>

          <label className="form-field">
            <span>Deployment location</span>

            <input
              name="deploymentLocation"
              value={form.deploymentLocation}
              onChange={updateField}
              minLength={2}
              maxLength={150}
              placeholder="Facility / Zone / Node"
              required
            />
          </label>

          <label className="form-field">
            <span>Sensor category</span>

            <select
              name="category"
              value={form.category}
              onChange={updateField}
            >
              <option value="Environmental">
                Environmental
              </option>

              <option value="PowerConsumption">
                Power consumption
              </option>

              <option value="Actuator">
                Actuator
              </option>
            </select>
          </label>

          <label className="form-field">
            <span>Payload data type</span>

            <select
              name="dataType"
              value={form.dataType}
              onChange={updateField}
            >
              <option value="Float">
                Float
              </option>

              <option value="Integer">
                Integer
              </option>

              <option value="Boolean">
                Boolean
              </option>
            </select>
          </label>

          <div className="form-guidance form-field--wide">
            <strong>Typed ingestion</strong>

            <p>
              Smart-X will accept only {form.dataType}
              {" "}telemetry for this sensor after
              registration.
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
                  Registering
                </>
              ) : (
                <>
                  <Cpu size={17} />
                  Register sensor
                </>
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}