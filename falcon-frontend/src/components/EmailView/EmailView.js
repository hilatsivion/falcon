import React, { useState, useEffect } from "react";
import "./EmailView.css";
import forwardingIcon from "../../assets/icons/black/forward-icon.svg";
import replyIcon from "../../assets/icons/black/reply-icon.svg";
import unreadIcon from "../../assets/icons/black/unread-icon.svg";
import trashIcon from "../../assets/icons/black/trash-red-icon.svg";
import backIcon from "../../assets/icons/black/arrow-left-20.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { Tag } from "../../pages/Main/Inbox/Inbox";
import { useNavigate } from "react-router-dom";

const EmailView = ({
  email,
  onClose,
  onDelete,
  onMarkUnread,
  onReply,
  onForward,
}) => {
  const navigate = useNavigate();
  const [isStarred, setIsStarred] = useState(email?.isStarred || false);

  useEffect(() => {
    if (email) setIsStarred(email.isStarred);
  }, [email]);

  const handleStarClick = () => {
    setIsStarred((prev) => !prev);
  };

  return (
    <div className={`email-view ${email ? "visible" : ""}`}>
      {email && (
        <>
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
                <Tag key={index} name={tag} />
              ))}
            </div>
          </div>
          {/* Email Body */}
          <div className="email-body">
            {email.body.split("\n").map((line, i) => (
              <p key={i}>{line}</p>
            ))}
          </div>

          {/* Toolbar */}
          <div className="email-toolbar">
            <button className="email-toolbar-item" onClick={onClose}>
              <img src={backIcon} alt="Back" />
              <span className="small-text">back</span>
            </button>

            <div className="flex-row-gap-30">
              <button
                className="email-toolbar-item"
                onClick={() => onMarkUnread(email)}
              >
                <img src={unreadIcon} alt="Mark as Unread" />
              </button>

              <button
                className="email-toolbar-item"
                onClick={() => onReply(email)}
              >
                <img src={replyIcon} alt="Reply" />
              </button>

              <button
                className="email-toolbar-item"
                onClick={() => onForward(email)}
              >
                <img src={forwardingIcon} alt="Forward" />
              </button>
            </div>

            <button
              className="email-toolbar-item trash-icon"
              onClick={() => onDelete(email)}
            >
              <img src={trashIcon} alt="Delete" />
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default EmailView;
