import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import "./connect.css";
import "../../../styles/global.css";

import logo from "../../../assets/images/falcon-white-full.svg";

// Animation Variants
const fadeIn = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5 } },
};

const errorAnimation = {
  hidden: { opacity: 0, y: -50 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5 } },
  exit: { opacity: 0, transition: { duration: 0.5 } },
};

const Login = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

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
    setError(message);
    setTimeout(() => {
      setError("");
    }, 3000);
  };

  // Handle Submit
  const handleSubmit = (e) => {
    e.preventDefault();
    if (validateForm()) {
      // Simulate API Call (Replace with actual login logic)
      navigate("/loadingData"); // Replace with your next page
    }
  };

  return (
    <div className="welcome-screen-container login-container">
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
        <motion.input
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        />

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
