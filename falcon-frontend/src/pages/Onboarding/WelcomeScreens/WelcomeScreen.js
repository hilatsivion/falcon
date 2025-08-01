import React, { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import "./WelcomeScreen.css";
import "../../../styles/global.css";

import logoFalcon from "../../../assets/images/falcon-white-full.png";
import welcomeImage from "../../../assets/images/image-mail-intro.png";

// Animation Variants
const headerVariants = {
  hidden: { opacity: 0, y: -50 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 1, ease: "easeInOut" },
  },
};

const imageVariants = {
  hidden: { opacity: 0, x: -100 },
  visible: {
    opacity: 1,
    x: 0,
    transition: { duration: 1, ease: "easeOut", delay: 0.6 },
  },
};

const buttonVariants = {
  hidden: { opacity: 0, y: 50 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 1, ease: "easeInOut", delay: 1 },
  },
};

const WelcomeScreen = () => {
  const navigate = useNavigate();

  // check if the user is already connected. if yes - go to inbox.
  useEffect(() => {
    const isAuthenticated = localStorage.getItem("isAuthenticated") === "true";
    if (isAuthenticated) {
      navigate("/inbox");
    }
  }, [navigate]);

  return (
    <div className="welcome-screen-container">
      <motion.div className="welcome-content">
        {/* Logo & Header */}
        <motion.div
          className="logo-full-white"
          variants={headerVariants}
          initial="hidden"
          animate="visible"
        >
          <img
            src={logoFalcon}
            alt="logo-falcon"
            className="logo-full-white-img"
          />
        </motion.div>
        <motion.p
          className="sub-title"
          variants={headerVariants}
          initial="hidden"
          animate="visible"
        >
          Take your Inbox to new heights
        </motion.p>

        {/* Image Sliding from Left */}
        <motion.img
          className="welcome-image"
          src={welcomeImage}
          alt="Welcome"
          variants={imageVariants}
          initial="hidden"
          animate="visible"
        />

        {/* Buttons Appearing from Bottom */}
        <motion.div
          className="flex-col"
          variants={buttonVariants}
          initial="hidden"
          animate="visible"
        >
          <motion.button
            className="btn-white"
            whileTap={{ scale: 0.95 }}
            onClick={() => navigate("/signup")}
            // onClick={() => navigate("/outlook-connect")}
          >
            Create Account
          </motion.button>
          <motion.div>
            <Link to="/login" className="login-link">
              Already have an account
            </Link>
          </motion.div>
        </motion.div>
      </motion.div>
    </div>
  );
};

export default WelcomeScreen;
