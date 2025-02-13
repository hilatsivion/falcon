import React from "react";
import { Link } from "react-router-dom";
import "./WelcomeScreen.css";
import logo from "../../../assets/images/falcon-white-full.svg";
import welcomeImage from "../../../assets/images/image-mail-intro.png";

const WelcomeScreen = () => {
  return (
    <div className="welcome-container">
      <div className="welcome-content">
        <img className="logo-full-white" src={logo} alt="logo-falcon" />
        <p className="tagline">Take your Inbox to new heights</p>
        <img className="welcome-image" src={welcomeImage} alt="Welcome" />
        <button className="create-account">Create Account</button>
        <Link to="/login" className="login-link">
          Already have an account
        </Link>
      </div>
    </div>
  );
};

export default WelcomeScreen;
