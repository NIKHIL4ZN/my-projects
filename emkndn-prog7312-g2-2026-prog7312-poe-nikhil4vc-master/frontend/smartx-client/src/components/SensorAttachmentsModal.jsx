import {
  Download,
  File,
  LoaderCircle,
  LockKeyhole,
  Paperclip,
  Upload,
  X
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";

import api from "../api/client";

function formatFileSize(bytes) {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(
    bytes /
    (1024 * 1024)
  ).toFixed(1)} MB`;
}

export default function SensorAttachmentsModal({
  sensor,
  onClose
}) {
  const [attachments, setAttachments] =
    useState([]);
  const [selectedFile, setSelectedFile] =
    useState(null);
  const [loading, setLoading] =
    useState(true);
  const [uploading, setUploading] =
    useState(false);
  const [error, setError] = useState("");

  const loadAttachments = useCallback(
    async () => {
      try {
        setError("");

        const response = await api.get(
          `/sensors/${sensor.id}/attachments`
        );

        setAttachments(response.data);
      } catch (requestError) {
        setError(requestError.message);
      } finally {
        setLoading(false);
      }
    },
    [sensor.id]
  );

  useEffect(() => {
    const timeoutId = window.setTimeout(
      () => {
        loadAttachments();
      },
      0
    );

    return () =>
      window.clearTimeout(timeoutId);
  }, [loadAttachments]);

  async function handleUpload(event) {
    event.preventDefault();

    const formElement = event.currentTarget;

    if (!selectedFile) {
      setError("Select a file to upload.");
      return;
    }

    const formData = new FormData();

    formData.append("file", selectedFile);

    try {
      setUploading(true);
      setError("");

      await api.post(
        `/sensors/${sensor.id}/attachments`,
        formData
      );

      setSelectedFile(null);

      formElement.reset();

      await loadAttachments();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setUploading(false);
    }
  }

  async function handleDownload(attachment) {
    try {
      setError("");

      const response = await api.get(
        `/sensors/${sensor.id}/attachments/` +
        `${attachment.id}/download`,
        {
          responseType: "blob"
        }
      );

      const objectUrl = URL.createObjectURL(
        response.data
      );

      const downloadLink =
        document.createElement("a");

      downloadLink.href = objectUrl;
      downloadLink.download =
        attachment.originalFileName;

      document.body.appendChild(downloadLink);
      downloadLink.click();
      downloadLink.remove();

      URL.revokeObjectURL(objectUrl);
    } catch (requestError) {
      setError(requestError.message);
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
        className="registration-modal attachment-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="attachment-title"
      >
        <header className="modal-header">
          <div className="modal-title">
            <span className="modal-icon">
              <Paperclip size={20} />
            </span>

            <div>
              <span className="section-label">
                Encrypted storage
              </span>

              <h2 id="attachment-title">
                Sensor attachments
              </h2>
            </div>
          </div>

          <button
            className="icon-button"
            onClick={onClose}
            type="button"
            aria-label="Close attachments"
          >
            <X size={19} />
          </button>
        </header>

        <p className="modal-description">
          Upload configuration files, deployment
          photographs, or hardware logs for{" "}
          <strong>{sensor.name}</strong>.
        </p>

        {error && (
          <div className="form-error">
            {error}
          </div>
        )}

        <form
          className="attachment-upload"
          onSubmit={handleUpload}
        >
          <label className="file-picker">
            <Upload size={19} />

            <span>
              {selectedFile
                ? selectedFile.name
                : "Choose an attachment"}
            </span>

            <input
              type="file"
              accept=".jpg,.jpeg,.png,.pdf,.txt,.log,.json,.csv,.yaml,.yml"
              onChange={(event) =>
                setSelectedFile(
                  event.target.files?.[0] ||
                  null
                )
              }
            />
          </label>

          <button
            className="primary-button"
            type="submit"
            disabled={
              uploading || !selectedFile
            }
          >
            {uploading ? (
              <>
                <LoaderCircle
                  className="spin"
                  size={17}
                />
                Encrypting
              </>
            ) : (
              <>
                <LockKeyhole size={17} />
                Upload securely
              </>
            )}
          </button>
        </form>

        <div className="encryption-notice">
          <LockKeyhole size={16} />

          <span>
            Files are streamed through AES-256
            encryption before being written to disk.
          </span>
        </div>

        <div className="attachment-list">
          {loading ? (
            <div className="empty-state attachment-empty">
              <LoaderCircle
                className="spin"
                size={25}
              />
              Loading attachments
            </div>
          ) : attachments.length === 0 ? (
            <div className="empty-state attachment-empty">
              <File size={26} />
              No files attached to this sensor
            </div>
          ) : (
            attachments.map((attachment) => (
              <article
                className="attachment-item"
                key={attachment.id}
              >
                <span className="attachment-file-icon">
                  <File size={20} />
                </span>

                <div className="attachment-details">
                  <strong>
                    {attachment.originalFileName}
                  </strong>

                  <span>
                    {formatFileSize(
                      attachment.fileSizeBytes
                    )}
                    {" · "}
                    {new Date(
                      attachment.uploadedAtUtc
                    ).toLocaleString()}
                  </span>
                </div>

                {attachment.isEncrypted && (
                  <span className="encrypted-badge">
                    <LockKeyhole size={11} />
                    Encrypted
                  </span>
                )}

                <button
                  className="icon-button"
                  type="button"
                  onClick={() =>
                    handleDownload(attachment)
                  }
                  aria-label={
                    `Download ${attachment.originalFileName}`
                  }
                >
                  <Download size={17} />
                </button>
              </article>
            ))
          )}
        </div>
      </section>
    </div>
  );
}