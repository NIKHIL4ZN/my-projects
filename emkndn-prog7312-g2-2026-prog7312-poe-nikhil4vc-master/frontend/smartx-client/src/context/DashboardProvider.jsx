import {
  useCallback,
  useMemo,
  useState
} from "react";

import DashboardContext from "./dashboardContext";

const selectedSensorStorageKey =
  "smartx-selected-sensor-id";

export default function DashboardProvider({
  children
}) {
  const [
    selectedSensorId,
    setSelectedSensorIdState
  ] = useState(
    () =>
      window.localStorage.getItem(
        selectedSensorStorageKey
      ) || ""
  );

  const setSelectedSensorId =
    useCallback((sensorId) => {
      setSelectedSensorIdState(sensorId);

      if (sensorId) {
        window.localStorage.setItem(
          selectedSensorStorageKey,
          sensorId
        );
      } else {
        window.localStorage.removeItem(
          selectedSensorStorageKey
        );
      }
    }, []);

  const value = useMemo(
    () => ({
      selectedSensorId,
      setSelectedSensorId
    }),
    [
      selectedSensorId,
      setSelectedSensorId
    ]
  );

  return (
    <DashboardContext.Provider
      value={value}
    >
      {children}
    </DashboardContext.Provider>
  );
}