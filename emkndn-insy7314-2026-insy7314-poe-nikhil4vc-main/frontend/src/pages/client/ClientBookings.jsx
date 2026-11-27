
import {
  useEffect,
  useState
} from "react";

import api from "../../api/api";
import Navbar from "../../components/Navbar";
import PageContainer from "../../components/PageContainer";

const bookingStatusInformation = {
  pending: {
    label: "Pending",
    description:
      "Your booking request is awaiting freelancer approval."
  },
  booked: {
    label: "Booked",
    description:
      "The freelancer accepted your request. Your booking is confirmed."
  },
  rejected: {
    label: "Rejected",
    description:
      "The freelancer declined your booking request."
  },
  completed: {
    label: "Completed",
    description:
      "This booking has been completed."
  },
  cancelled: {
    label: "Cancelled",
    description:
      "This booking has been cancelled."
  }
};

export default function ClientBookings() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadBookings() {
      try {
        const response = await api.get(
          "/bookings/mine"
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

  return (
    <>
      <Navbar />

      <PageContainer>
        <section className="page-header">
          <h1>My Bookings</h1>

          <p className="page-subtitle">
            Track your booking requests and
            freelancer approval decisions.
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

        {!loading &&
          !error &&
          bookings.length === 0 && (
            <div className="empty-state">
              You have not requested any gigs yet.
            </div>
          )}

        {!loading &&
          !error &&
          bookings.length > 0 && (
            <div className="booking-list">
              {bookings.map((booking) => {
                const statusInformation =
                  bookingStatusInformation[booking.status] || {
                    label: booking.status || "Unknown",
                    description:
                      "The current booking status is unavailable."
                  };

                return (
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
                          Freelancer:{" "}
                          {booking.freelancer?.name ||
                            "Unknown freelancer"}
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
                      <div>
                        <span
                          className={`status-badge status-${booking.status}`}
                        >
                          {statusInformation.label}
                        </span>

                        <p className="booking-status-description">
                          {statusInformation.description}
                        </p>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
      </PageContainer>
    </>
  );
}
