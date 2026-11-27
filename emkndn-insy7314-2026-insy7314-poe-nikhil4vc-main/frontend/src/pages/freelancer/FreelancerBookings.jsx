
import {
  useEffect,
  useState
} from "react";

import api from "../../api/api";
import Navbar from "../../components/Navbar";
import PageContainer from "../../components/PageContainer";

export default function FreelancerBookings() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [processingBookingId, setProcessingBookingId] = useState(null);

  useEffect(() => {
    async function loadBookings() {
      try {
        const response = await api.get(
          "/bookings/freelancer"
        );

        setBookings(response.data.bookings);
      } catch (requestError) {
        setError(
          requestError.response?.data?.message ||
          "Unable to load bookings."
        );
      } finally {
        setLoading(false);
      }
    }

    loadBookings();
  }, []);

  async function handleBookingDecision(bookingId, decision) {
    if (processingBookingId !== null) {
      return;
    }

    setProcessingBookingId(bookingId);
    setActionError("");
    setSuccessMessage("");

    try {
      const response = await api.patch(
        `/bookings/${bookingId}/${decision}`
      );

      const updatedBooking = response.data.booking;

      setBookings((previousBookings) =>
        previousBookings.map((booking) =>
          booking._id === bookingId
            ? {
                ...booking,
                status: updatedBooking.status
              }
            : booking
        )
      );

      setSuccessMessage(
        response.data.message ||
        (decision === "accept"
          ? "Booking request accepted successfully."
          : "Booking request rejected.")
      );
    } catch (requestError) {
      setActionError(
        requestError.response?.data?.message ||
        "Unable to process the booking request."
      );
    } finally {
      setProcessingBookingId(null);
    }
  }

  return (
    <>
      <Navbar />

      <PageContainer>
        <section className="page-header">
          <h1>Client Booking Requests</h1>

          <p className="page-subtitle">
            Review client requests for your services.
            Accept or reject pending bookings.
          </p>
        </section>

        {loading && (
          <p>Loading bookings...</p>
        )}

        {error && (
          <div
            className="error-message"
            role="alert"
          >
            {error}
          </div>
        )}

        {actionError && (
          <div
            className="error-message"
            role="alert"
          >
            {actionError}
          </div>
        )}

        {successMessage && (
          <div
            className="success-message"
            role="status"
          >
            {successMessage}
          </div>
        )}

        {!loading &&
          !error &&
          bookings.length === 0 && (
            <div className="empty-state">
              <h2>No booking requests yet</h2>

              <p>
                Client requests for your gigs will
                appear here.
              </p>
            </div>
          )}

        {!loading &&
          !error &&
          bookings.length > 0 && (
            <div className="booking-list">
              {bookings.map((booking) => (
                <article
                  className="card booking-card"
                  key={booking._id}
                >
                  <div className="booking-main">
                    <div>
                      <span className="gig-category">
                        {booking.gig?.category ||
                          "Service"}
                      </span>

                      <h2>
                        {booking.gig?.title ||
                          "Gig"}
                      </h2>

                      <p className="booking-freelancer">
                        Client:{" "}
                        {booking.client?.name ||
                          "Unknown client"}
                      </p>
                    </div>

                    <div className="booking-price-block">
                      <span className="meta-label">
                        Price
                      </span>

                      <strong>
                        R{booking.price}
                      </strong>
                    </div>
                  </div>

                  <div className="booking-footer">
                    <span
                      className={`status-badge status-${booking.status}`}
                    >
                      {booking.status}
                    </span>

                    {booking.status === "pending" && (
                      <div className="booking-actions">
                        <button
                          type="button"
                          className="button button-small booking-accept-button"
                          disabled={processingBookingId !== null}
                          onClick={() =>
                            handleBookingDecision(
                              booking._id,
                              "accept"
                            )
                          }
                        >
                          {processingBookingId === booking._id
                            ? "Processing..."
                            : "Accept"}
                        </button>

                        <button
                          type="button"
                          className="button button-small booking-reject-button"
                          disabled={processingBookingId !== null}
                          onClick={() =>
                            handleBookingDecision(
                              booking._id,
                              "reject"
                            )
                          }
                        >
                          Reject
                        </button>
                      </div>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
      </PageContainer>
    </>
  );
}
