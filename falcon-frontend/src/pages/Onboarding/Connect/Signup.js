import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";

import "./connect.css";
import "../../../styles/global.css";

import eyeOpenIcon from "../../../assets/icons/black/eye-open.svg";
import eyeClosedIcon from "../../../assets/icons/black/eye-closed.svg";
import logo from "../../../assets/images/falcon-white-full.svg";
import { API_BASE_URL } from "../../../config/constants";
import Loader from "../../../components/Loader/Loader";
import { toast } from "react-toastify";
import { useAuth } from "../../../context/AuthContext";
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

const SignUp = () => {
  const [username, setUsername] = useState("");
  const [fullname, setFullname] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();
  const { login } = useAuth();

  const showError = (message) => {
    const audio = new Audio(errorSound);
    audio.play();

    toast.error(message, {
      position: "top-right",
      autoClose: 4000,
      hideProgressBar: false,
      pauseOnHover: true,
      draggable: true,
    });
  };

  // generic function for validate all fields
  const validateField = (value, regex, message) => {
    if (!regex.test(value)) {
      showError(message);
      return false;
    }
    return true;
  };

  const validateForm = () => {
    if (
      !validateField(
        username,
        /^[a-zA-Z0-9]{3,}$/,
        "Username must be at least 3 letters in English and contain no special characters."
      )
    )
      return false;

    if (
      !validateField(
        fullname.trim(),
        /^[A-Za-z]+(?: [A-Za-z]+)+$/,
        "Full name must contain at least two alphabetic words."
      )
    )
      return false;

    if (
      !validateField(
        email,
        /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        "Please enter a valid email address."
      )
    )
      return false;

    if (
      !validateField(
        password,
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/,
        "Password must contain at least one uppercase letter, one lowercase letter, and a number."
      )
    )
      return false;

    if (password !== confirmPassword) {
      showError("Passwords do not match.");
      return false;
    }

    return true;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;
    setIsLoading(true);

    try {
      const signUpRes = await fetch(`${API_BASE_URL}/api/auth/signup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fullname, username, email, password }),
      });

      if (!signUpRes.ok) {
        let errorText = await signUpRes.text();
        try {
          const errorData = JSON.parse(errorText);
          showError(errorData.message || "Signup failed.");
        } catch {
          showError(`Signup failed: ${errorText}`);
        }
        return;
      }

      const data = await signUpRes.json();

      if (data && data.token && data.aiKey) {
        login(data.token, data.aiKey);
        navigate("/interests");
      } else {
        showError("Signup succeeded, but failed to retrieve session token.");
      }
    } catch (err) {
      console.error("Signup Error:", err);
      showError("Signup failed. Check network or try again later.");
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword((prev) => !prev);
  };

  return (
    <div className="welcome-screen-container signup-container">
      {isLoading && <Loader />}

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
          type="text"
          placeholder="Full name"
          value={fullname}
          onChange={(e) => setFullname(e.target.value)}
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
            type={showPassword ? "text" : "password"}
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

        <motion.p
          className="signup-login"
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          Already registered? <Link to="/login">Login</Link>
        </motion.p>
      </motion.div>

      <button className="btn-white btn-create" onClick={handleSubmit}>
        Create
      </button>
    </div>
  );
};

export default SignUp;
