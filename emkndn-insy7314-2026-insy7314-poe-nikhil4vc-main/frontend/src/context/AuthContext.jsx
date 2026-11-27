import {
    useEffect,
    useState
} from "react";

import api from "../api/api";
import { AuthContext } from "./authContextInstance";

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        async function loadUser() {
            const token = sessionStorage.getItem("token");

            if (!token) {
                setLoading(false);
                return;
            }

            try {
                const response = await api.get("/auth/me");
                setUser(response.data.user);
            } catch {
                sessionStorage.removeItem("token");
                setUser(null);
            } finally {
                setLoading(false);
            }
        }

        loadUser();
    }, []);

    async function login(email, password) {
        const response = await api.post("/auth/login", {
            email,
            password
        });

        sessionStorage.setItem(
            "token",
            response.data.token
        );

        setUser(response.data.user);

        return response.data;
    }

   async function logout() {
    try {
        await api.post("/auth/logout");
    } catch (error) {
        console.error(
            "Logout request failed:",
            error
        );
    } finally {
        sessionStorage.removeItem("token");
        setUser(null);
    }
}

    return (
        <AuthContext.Provider
            value={{
                user,
                loading,
                login,
                logout
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}