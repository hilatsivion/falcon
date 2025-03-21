import React from "react";
import "./EmailView.css";
import moreIcon from "../../assets/icons/black/more-icon.svg";
import forwardingIcon from "../../assets/icons/black/forward-icon.svg";
import replyIcon from "../../assets/icons/black/reply-icon.svg";

const EmailView = ({ email, onClose }) => {
  return (
    <div className={`email-view ${email ? "visible" : ""}`}>
      {email && (
        <>
          {/* Email Header */}
          <div className="email-detail">
            <div className="email-detail-header">
              <div className="email-sender-container">
                <div
                  className="email-avatar"
                  style={{ backgroundColor: email.avatarColor || "#ccc" }}
                >
                  {email.sender.charAt(0).toUpperCase()}
                </div>
                <span className="email-sender">{email.sender}</span>
              </div>
              <img
                src={moreIcon}
                alt="More options"
                className="email-more-icon"
              />
            </div>

            <div className="email-subject">{email.subject}</div>

            <div className="email-tags">
              {email.tags.map((tag, index) => (
                <span key={index} className="email-tag">
                  {tag}
                </span>
              ))}
            </div>
          </div>

          <div className="email-tools">
            <img
              src={replyIcon}
              alt="More options"
              className="email-more-icon"
            />
            <img
              src={forwardingIcon}
              alt="More options"
              className="email-more-icon"
            />
          </div>

          {/* Placeholder for Email Body */}
          <div className="email-body">
            <p>Dear readers,</p>
            <p>This is an example of a compose email.</p>
            <p>More words can be here.</p>
            <p>Thanks,</p>
            <p>Hila Tsivion</p>
          </div>
        </>
      )}
    </div>
  );
};

export default EmailView;
