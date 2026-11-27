
import {
  useEffect,
  useState
} from "react";

import api from "../../api/api";
import Navbar from "../../components/Navbar";
import PageContainer from "../../components/PageContainer";

export default function Income() {
  const [income, setIncome] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadIncome() {
      try {
        const response = await api.get(
          "/bookings/income"
        );

        setIncome(response.data);
      } catch (requestError) {
        setError(
          requestError.response?.data?.message ||
          "Unable to load income."
        );
      } finally {
        setLoading(false);
      }
    }

    loadIncome();
  }, []);

  function formatCurrency(value) {
    return new Intl.NumberFormat("en-ZA", {
      style: "currency",
      currency: "ZAR"
    }).format(value || 0);
  }

  return (
    <>
      <Navbar />

      <PageContainer>
        <section className="page-header">
          <h1>Financial Dashboard</h1>

          <p className="page-subtitle">
            Review your earnings, estimated tax
            and completed transactions.
          </p>
        </section>

        {loading && (
          <p>Loading financial information...</p>
        )}

        {error && (
          <div
            className="error-message"
            role="alert"
          >
            {error}
          </div>
        )}

        {!loading && !error && income && (
          <>
            <div className="income-summary">
              <section className="card summary-card">
                <span className="meta-label">
                  Gross Income
                </span>

                <strong className="summary-value">
                  {formatCurrency(
                    income.financialSummary.grossIncome
                  )}
                </strong>
              </section>

              <section className="card summary-card">
                <span className="meta-label">
                  Estimated Tax
                </span>

                <strong className="summary-value">
                  {formatCurrency(
                    income.financialSummary.estimatedTax
                  )}
                </strong>
              </section>

              <section className="card summary-card">
                <span className="meta-label">
                  Estimated Net Income
                </span>

                <strong className="summary-value">
                  {formatCurrency(
                    income.financialSummary.estimatedNetIncome
                  )}
                </strong>
              </section>

              <section className="card summary-card">
                <span className="meta-label">
                  Flat Tax Rate
                </span>

                <strong className="summary-value">
                  15%
                </strong>
              </section>
            </div>

            <section className="card tax-information">
              <div className="section-header">
                <div>
                  <h2>Tax Estimate</h2>

                  <p>
                    HustleHub+ provides an approximate
                    tax estimate based on your completed
                    marketplace income.
                  </p>
                </div>
              </div>

              <div className="flat-tax-container">
                <div className="flat-tax-details">
                  <span className="meta-label">
                    Flat Estimated Tax Rate
                  </span>

                  <strong className="flat-tax-percentage">
                    15%
                  </strong>
                </div>

                <p className="flat-tax-description">
                  A fixed 15% tax estimate is applied
                  to all completed simulated booking
                  payments, regardless of the total
                  income earned.
                </p>
              </div>

              <p className="flat-tax-disclaimer">
                This is an educational tax estimate,
                not an official South African tax
                assessment.
              </p>
            </section>

            <section className="card financial-visualisation">
              <div className="section-header">
                <div>
                  <h2>Income Breakdown</h2>

                  <p>
                    See how your gross income is divided
                    between estimated tax and net income.
                  </p>
                </div>
              </div>

              <div className="income-breakdown">
                <div className="breakdown-bar">
                  <div
                    className="breakdown-net"
                    style={{
                      width: `${
                        income.financialSummary.grossIncome > 0
                          ? (
                              income.financialSummary.estimatedNetIncome /
                              income.financialSummary.grossIncome
                            ) * 100
                          : 0
                      }%`
                    }}
                  />

                  <div
                    className="breakdown-tax"
                    style={{
                      width: `${
                        income.financialSummary.grossIncome > 0
                          ? (
                              income.financialSummary.estimatedTax /
                              income.financialSummary.grossIncome
                            ) * 100
                          : 0
                      }%`
                    }}
                  />
                </div>

                <div className="breakdown-details">
                  <div className="breakdown-item">
                    <span className="breakdown-indicator net-indicator" />

                    <span className="breakdown-label">
                      Estimated Net Income
                    </span>

                    <strong className="breakdown-value">
                      {formatCurrency(
                        income.financialSummary.estimatedNetIncome
                      )}
                    </strong>
                  </div>

                  <div className="breakdown-item">
                    <span className="breakdown-indicator tax-indicator" />

                    <span className="breakdown-label">
                      Estimated Tax
                    </span>

                    <strong className="breakdown-value">
                      {formatCurrency(
                        income.financialSummary.estimatedTax
                      )}
                    </strong>
                  </div>
                </div>
              </div>
            </section>

            <div className="transactions-section">
              <section className="section-header transactions-header">
                <div>
                  <h2>Transactions</h2>

                  <p>
                    Your completed booking payments.
                  </p>
                </div>

                <span className="status-badge">
                  {income.transactionCount} completed
                </span>
              </section>
            </div>

            {income.transactions.length === 0 ? (
              <div className="empty-state">
                <h2>No transactions yet</h2>

                <p>
                  Completed payments will appear here.
                </p>
              </div>
            ) : (
              <div className="transaction-list">
                {income.transactions.map(
                  (transaction) => (
                    <article
                      className="card transaction-card"
                      key={transaction._id}
                    >
                      <div className="transaction-main">
                        <div>
                          <span className="meta-label">
                            Payment
                          </span>

                          <h3>
                            {formatCurrency(
                              transaction.amount
                            )}
                          </h3>
                        </div>

                        <span
                          className={`status-badge status-${transaction.status}`}
                        >
                          {transaction.status}
                        </span>
                      </div>

                      <div className="transaction-details">
                        <div>
                          <span className="meta-label">
                            Type
                          </span>

                          <p>
                            {transaction.type ===
                            "booking_payment"
                              ? "Booking Payment"
                              : transaction.type}
                          </p>
                        </div>

                        <div>
                          <span className="meta-label">
                            Date
                          </span>

                          <p>
                            {new Date(
                              transaction.createdAt
                            ).toLocaleString()}
                          </p>
                        </div>
                      </div>
                    </article>
                  )
                )}
              </div>
            )}
          </>
        )}
      </PageContainer>
    </>
  );
}
