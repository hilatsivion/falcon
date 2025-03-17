import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import "./connect.css";
import "../../../styles/global.css";

import eyeOpenIcon from "../../../assets/icons/black/eye-open.svg";
import eyeClosedIcon from "../../../assets/icons/black/eye-closed.svg";

import logo from "../../../assets/images/falcon-white-full.svg";
import errorSound from "../../../assets/sounds/error-message.mp3";

// Animation Variants
const fadeIn = {
  hidden: { opacity: 0, y: 30 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.8 } },
};
const fadeInEye = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: 0.4, delay: 0.7 } },
};

const errorAnimation = {
  hidden: { opacity: 0, y: -50 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5 } },
  exit: { opacity: 0, transition: { duration: 0.5 } },
};

const SignUp = () => {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const navigate = useNavigate();

  // Validation Function
  const validateForm = () => {
    const usernameRegex = /^[a-zA-Z0-9]{3,}$/; // At least 3 letters, no special characters
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/; // Valid email format
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/; // At least 1 uppercase, 1 lowercase, and 1 number

    if (!usernameRegex.test(username)) {
      showError(
        "Username must be at least 3 letters in English and contain no special characters."
      );
      return false;
    }

    if (!emailRegex.test(email)) {
      showError("Please enter a valid email address.");
      return false;
    }

    if (!passwordRegex.test(password)) {
      showError(
        "Password must contain at least one uppercase letter, one lowercase letter, and a number."
      );
      return false;
    }

    if (password !== confirmPassword) {
      showError("Passwords do not match.");
      return false;
    }

    return true;
  };

  // Show Error for 5 seconds
  const showError = (message) => {
    setError(message);
    const audio = new Audio(errorSound);
    audio.play();
    setTimeout(() => {
      setError("");
    }, 5000);
  };

  // Handle Submit
  const handleSubmit = (e) => {
    e.preventDefault();
    if (validateForm()) {
      // API call to server, and then:
      navigate("/interests");
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword((prev) => !prev);
  };

  return (
    <div className="welcome-screen-container signup-container">
      {/* Error Popup */}
      <AnimatePresence>
        {error && (
          <motion.div
            className="error-popup"
            variants={errorAnimation}
            initial="hidden"
            animate="visible"
            exit="exit"
          >
            {error}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Logo */}
      <motion.img
        className="logo-full-white-small"
        src={logo}
        alt="logo-falcon"
      />

      <motion.div
        className="signup-card"
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 1 }}
      >
        {/* Title */}
        <motion.h2
          className="signup-title"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Get Started
        </motion.h2>
        <motion.p
          className="sub-title"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Welcome to Falcon - Let’s create your account
        </motion.p>

        {/* Form Inputs */}
        <motion.input
          type="text"
          placeholder="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        />
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
            <img
              src={showPassword ? eyeOpenIcon : eyeClosedIcon}
              alt="Toggle Password"
            />
          </motion.button>
        </motion.div>
        <motion.input
          type="password"
          placeholder="Confirm Password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        />

        {/* Already Registered? */}
        <motion.p
          className="signup-login"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Already registered? <Link to="/login">Login</Link>
        </motion.p>

        {/* Create Account Button */}
      </motion.div>

      <button className="btn-white btn-create" onClick={handleSubmit}>
        Create
      </button>
    </div>
  );
};

export default SignUp;
