import React, { useState, useEffect } from "react";
import "./EmailView.css";
import forwardingIcon from "../../assets/icons/black/forward-icon.svg";
import replyIcon from "../../assets/icons/black/reply-icon.svg";
import unreadIcon from "../../assets/icons/black/unread-icon.svg";
import trashIcon from "../../assets/icons/black/trash-red-icon.svg";
import backIcon from "../../assets/icons/black/arrow-left-20.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";

const EmailView = ({ email, onClose }) => {
  const [isStarred, setIsStarred] = useState(email?.isStarred || false);

  useEffect(() => {
    if (email) setIsStarred(email.isStarred);
  }, [email]);

  const handleStarClick = () => {
    setIsStarred((prev) => !prev);
    // optional: if you want to update the main inbox state too,
    // you can call a parent-provided `onToggleStar(email)` here
  };

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
              <span className="email-time">{email.time}</span>
            </div>

            <div className="email-header-view">
              <h3 className="email-subject">{email.subject}</h3>

              <div className="email-star" onClick={handleStarClick}>
                {isStarred ? <StarIconFull /> : <StarIconEmpty />}
              </div>
            </div>

            <div className="email-tags">
              {email.tags.map((tag, index) => (
                <span key={index} className="email-tag">
                  {tag}
                </span>
              ))}
            </div>
          </div>

          {/* Email Body */}
          <div className="email-body">
            <p>Dear readers,</p>
            <p>This is an example of a compose email.</p>
            <p>More words can be here.</p>
            <p>Thanks,</p>
            <p>Hila Tsivion</p>
          </div>

          {/* Toolbar */}
          <div className="email-toolbar">
            <button className="email-toolbar-item" onClick={onClose}>
              <img src={backIcon} alt="Back" />
              <span className="small-text">back</span>
            </button>

            <div className="flex-row-gap-20">
              <button className="email-toolbar-item">
                <img src={unreadIcon} alt="Mark as Unread" />
              </button>
              <button className="email-toolbar-item">
                <img src={replyIcon} alt="Reply" />
              </button>
              <button className="email-toolbar-item">
                <img src={forwardingIcon} alt="Forward" />
              </button>
            </div>

            <button className="email-toolbar-item trash-icon">
              <img src={trashIcon} alt="Delete" />
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default EmailView;
