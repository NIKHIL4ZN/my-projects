import {
  Navigate,
  Route,
  Routes
} from "react-router-dom";

import DashboardProvider from "./context/DashboardProvider";
import LandingPage from "./pages/LandingPage";
import TelemetryDashboard from "./pages/TelemetryDashboard";
import CommandStreamDashboard from "./pages/CommandStreamDashboard";
import TopologyDashboard from "./pages/TopologyDashboard";
import AboutPage from "./pages/AboutPage";
import OverviewDashboard from "./pages/OverviewDashboard";

export default function App() {
  return (
    <DashboardProvider>
      <Routes>
        <Route
          path="/"
          element={<LandingPage />}
        />

        <Route
          path="/telemetry"
          element={<TelemetryDashboard />}
        />

        <Route
          path="/commands"
          element={<CommandStreamDashboard />}
        />

<Route
  path="/topology"
  element={<TopologyDashboard />}
/>

<Route
  path="/about"
  element={<AboutPage />}
/>

<Route
  path="/dashboard"
  element={<OverviewDashboard />}
/>

        <Route
          path="*"
          element={
            <Navigate to="/" replace />
          }
        />
      </Routes>
    </DashboardProvider>
  );
}