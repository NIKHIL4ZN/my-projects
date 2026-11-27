
import {
  useEffect,
  useState
} from "react";

import {
  Link,
  useParams
} from "react-router-dom";

import api from "../../api/api";
import { useAuth } from "../../context/useAuth";
import Navbar from "../../components/Navbar";
import PageContainer from "../../components/PageContainer";

export default function GigDetails() {
  const { id } = useParams();
  const { user } = useAuth();

  const [gig, setGig] = useState(null);
  const [bookingStatus, setBookingStatus] =
    useState(null);

  const [loading, setLoading] = useState(true);
  const [booking, setBooking] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] =
    useState("");

  useEffect(() => {
    let cancelled = false;

    async function loadGig() {
      setLoading(true);
      setError("");
      setBookingStatus(null);
      setSuccessMessage("");

      try {
        const gigResponse = await api.get(
          `/gigs/${id}`
        );

        if (cancelled) return;

        setGig(gigResponse.data.gig);

        if (user?.role === "client") {
          const bookingsResponse =
            await api.get("/bookings/mine");

          if (cancelled) return;

          const activeBooking =
            bookingsResponse.data.bookings.find(
              (existingBooking) =>
                ["pending", "booked"].includes(
                  existingBooking.status
                ) &&
                (
                  existingBooking.gig?._id === id ||
                  existingBooking.gig === id
                )
            );

          setBookingStatus(
            activeBooking?.status || null
          );
        }
      } catch (requestError) {
        if (!cancelled) {
          setError(
            requestError.response?.data?.message ||
            "Unable to load gig."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadGig();

    return () => {
      cancelled = true;
    };
  }, [id, user?.role]);

  async function handleBooking() {
    if (
      booking ||
      bookingStatus === "pending" ||
      bookingStatus === "booked"
    ) {
      return;
    }

    setError("");
    setSuccessMessage("");
    setBooking(true);

    try {
      const response = await api.post(
        "/bookings",
        {
          gigId: id
        }
      );

      setBookingStatus(
        response.data.booking?.status || "pending"
      );

      setSuccessMessage(
        "Booking request submitted successfully. " +
        "The freelancer must accept your request " +
        "before the booking is confirmed."
      );
    } catch (requestError) {
      const message =
        requestError.response?.data?.message ||
        "Unable to submit your booking request.";

      setError(message);

      if (requestError.response?.status === 409) {
        try {
          const bookingsResponse =
            await api.get("/bookings/mine");

          const activeBooking =
            bookingsResponse.data.bookings.find(
              (existingBooking) =>
                ["pending", "booked"].includes(
                  existingBooking.status
                ) &&
                (
                  existingBooking.gig?._id === id ||
                  existingBooking.gig === id
                )
            );

          setBookingStatus(
            activeBooking?.status || null
          );
        } catch {
          // Keep the original booking error visible.
        }
      }
    } finally {
      setBooking(false);
    }
  }

  return (
    <>
      <Navbar />

      <PageContainer narrow>
        <Link
          to="/gigs"
          className="back-link"
        >
          ← Back to marketplace
        </Link>

        {loading && (
          <p>Loading gig...</p>
        )}

        {!loading && error && !gig && (
          <div
            className="error-message"
            role="alert"
          >
            {error}
          </div>
        )}

        {!loading && gig && (
          <article className="card gig-details-card">
            <span className="gig-category">
              {gig.category}
            </span>

            <h1>{gig.title}</h1>

            <p className="gig-details-description">
              {gig.description}
            </p>

            <div className="gig-details-meta">
              <div>
                <span className="meta-label">
                  Freelancer
                </span>

                <strong>
                  {gig.freelancer?.name ||
                    "Unavailable"}
                </strong>
              </div>

              <div>
                <span className="meta-label">
                  Price
                </span>

                <strong className="gig-details-price">
                  R{gig.price}
                </strong>
              </div>
            </div>

            {error && (
              <div
                className="error-message"
                role="alert"
              >
                {error}
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

            {user?.role === "client" && (
              <>
                {bookingStatus === "pending" ? (
                  <button
                    type="button"
                    className="button booking-button"
                    disabled
                  >
                    Request Pending
                  </button>
                ) : bookingStatus === "booked" ? (
                  <button
                    type="button"
                    className="button booking-button"
                    disabled
                  >
                    Already Booked
                  </button>
                ) : (
                  <button
                    type="button"
                    className="button booking-button"
                    onClick={handleBooking}
                    disabled={booking}
                  >
                    {booking
                      ? "Submitting Request..."
                      : `Request Booking for R${gig.price}`}
                  </button>
                )}

                <p className="booking-status-description">
                  {bookingStatus === "pending"
                    ? "Your request is awaiting freelancer approval."
                    : bookingStatus === "booked"
                      ? "The freelancer has accepted your booking."
                      : "The freelancer must accept your request before the booking is confirmed."}
                </p>

                <Link
                  to="/bookings"
                  className="back-link"
                >
                  View My Bookings
                </Link>
              </>
            )}
          </article>
        )}
      </PageContainer>
    </>
  );
}
