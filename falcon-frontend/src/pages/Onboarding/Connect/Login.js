import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import "./connect.css";
import "../../../styles/global.css";

import { ReactComponent as EyeOpenIcon } from "../../../assets/icons/black/eye-open.svg";
import { ReactComponent as EyeClosedIcon } from "../../../assets/icons/black/eye-closed.svg";

import logoFalcon from "../../../assets/images/falcon-white-full.png";
import errorSound from "../../../assets/sounds/error-message.mp3";

import Loader from "../../../components/Loader/Loader";
import { API_BASE_URL } from "../../../config/constants";
import { useAuth } from "../../../context/AuthContext";
import { toast } from "react-toastify";

import "react-toastify/dist/ReactToastify.css";
import "../../../styles/toastify-custom.css";

// Animation Variants
const fadeIn = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5 } },
};
const fadeInEye = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: 0.4, delay: 0.7 } },
};

const Login = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  // Validation Function
  const validateForm = () => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/; // Valid email format
    if (!emailRegex.test(email)) {
      showError("Please enter a valid email address.");
      return false;
    }
    if (password.length < 6) {
      showError("Password must be at least 6 characters.");
      return false;
    }
    return true;
  };

  // Show Error for 3 seconds
  const showError = (message) => {
    const audio = new Audio(errorSound);
    audio.play().catch(() => {
      /* ignore audio errors */
    });
    toast.error(message, {
      position: "top-right",
      autoClose: 3000,
      hideProgressBar: false,
      pauseOnHover: true,
      draggable: true,
    });
  };

  // Handle Submit
  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) return;

    setIsLoading(true);

    try {
      const loginRes = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      // --- Check if login failed ---
      if (!loginRes.ok) {
        let backendError = `Login failed (${loginRes.status})`;
        const errorBodyText = await loginRes.text();
        try {
          const errorData = JSON.parse(errorBodyText);
          backendError =
            errorData?.message ||
            backendError + ": Invalid credentials or server error.";
        } catch (parseError) {
          backendError =
            backendError +
            (errorBodyText
              ? `: ${errorBodyText}`
              : ": Server returned an error.");
          console.warn(
            "Could not parse error response as JSON:",
            errorBodyText
          );
        }
        throw new Error(backendError); // Throw error to be caught below
      }

      const data = await loginRes.json();

      if (data && data.token && data.aiKey) {
        login(data.token, data.aiKey);
        navigate("/inbox");
      } else {
        console.error("Login response OK but token missing:", data);
        throw new Error(
          "Login succeeded, but failed to retrieve session token."
        );
      }
    } catch (err) {
      console.error("Login Process Error:", err);
      showError(
        err.message || "Login failed. Check network or try again later."
      );
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword((prev) => !prev);
  };

  return (
    <div className="welcome-screen-container login-container">
      {isLoading && <Loader />}

      {/* Logo */}
      <motion.img
        className="logo-full-white-small"
        src={logoFalcon}
        alt="logo-falcon"
      />

      <motion.div
        className="login-card"
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 1 }}
      >
        {/* Title */}
        <motion.h2
          className="login-title"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Welcome Back!
        </motion.h2>
        <motion.p
          className="sub-title"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Enter your details below
        </motion.p>

        {/* Form Inputs */}
        <motion.input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        />
        <motion.div className="password-container">
          <motion.input
            type={showPassword ? "text" : "password"} // Toggle between text & password
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            variants={fadeIn}
            initial="hidden"
            animate="visible"
          />
          <motion.button
            type="button"
            className="toggle-password-btn"
            onClick={togglePasswordVisibility}
            variants={fadeInEye}
            initial="hidden"
            animate="visible"
          >
            {showPassword ? <EyeOpenIcon /> : <EyeClosedIcon />}
          </motion.button>
        </motion.div>

        {/* Not Registered Yet? */}
        <motion.p
          className="signup-login"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Not registered yet? <Link to="/signup">Sign up</Link>
        </motion.p>
      </motion.div>

      {/* Log In Button */}
      <button className="btn-white btn-login" onClick={handleSubmit}>
        Log in
      </button>
    </div>
  );
};

export default Login;
