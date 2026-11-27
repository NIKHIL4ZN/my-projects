import axios from "axios";

const api = axios.create({
  baseURL:
    import.meta.env.VITE_API_BASE_URL ||
    "http://localhost:5229/api",
  timeout: 15000,
  headers: {
    Accept: "application/json"
  }
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const message =
      error.response?.data?.message ||
      error.response?.data?.title ||
      error.message ||
      "The Smart-X API request failed.";

    return Promise.reject(
      new Error(message)
    );
  }
);

export default api;