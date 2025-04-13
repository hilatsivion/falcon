import "./EmailView.css";
import forwardingIcon from "../../assets/icons/black/forward-icon.svg";
import replyIcon from "../../assets/icons/black/reply-icon.svg";
import unreadIcon from "../../assets/icons/black/unread-icon.svg";
import trashIcon from "../../assets/icons/black/trash-red-icon.svg";
import backIcon from "../../assets/icons/black/arrow-left-20.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { Tag } from "../../pages/Main/Inbox/Inbox";

const EmailView = ({
  email,
  onClose,
  onDelete,
  onMarkUnread,
  onReply,
  onForward,
  onToggleFavorite,
}) => {
  if (!email) {
    return null;
  }

  const handleStarClick = (e) => {
    e.stopPropagation();
    if (onToggleFavorite) {
      onToggleFavorite();
    } else {
      console.warn("EmailView: onToggleFavorite handler is missing");
    }
  };

  const senderInitial = email?.sender
    ? email.sender.charAt(0).toUpperCase()
    : "?";
  const hashCode = (str) => {
    let hash = 0;
    if (!str) return hash;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    return hash;
  };
  const intToRGB = (i) => {
    const c = (i & 0x00ffffff).toString(16).toUpperCase();
    return "00000".substring(0, 6 - c.length) + c;
  };
  const avatarColorCalculated =
    email?.avatarColor || `#${intToRGB(hashCode(email?.sender || ""))}`;

  const formatRecipients = (recipients) => {
    if (!Array.isArray(recipients)) return "";
    return recipients.map((r) => r.email || r).join(", ");
  };

  const formattedTime = email?.timeReceived
    ? new Date(email.timeReceived).toLocaleString()
    : "N/A";

  const hasAttachments =
    Array.isArray(email.attachments) && email.attachments.length > 0;

  return (
    <div
      className={`email-view-overlay ${email ? "visible" : ""}`}
      onClick={onClose}
    >
      <div
        className={`email-view ${email ? "visible" : ""}`}
        onClick={(e) => e.stopPropagation()}
      >
        {email && (
          <>
            <div className="email-detail">
              <div className="email-detail-header">
                <div className="email-sender-container">
                  <div
                    className="email-avatar"
                    style={{ backgroundColor: avatarColorCalculated }}
                  >
                    {senderInitial}
                  </div>
                  <div className="sender-recipient-details">
                    <span className="email-sender">{email.sender}</span>
                    <span className="email-recipients">
                      To: {formatRecipients(email.recipients)}
                    </span>
                  </div>
                </div>
                <span className="email-time">{formattedTime}</span>
              </div>

              <div className="email-header-view">
                <h3 className="email-subject">
                  {email.subject || "(No Subject)"}
                </h3>
                <div className="email-star" onClick={handleStarClick}>
                  {email.isFavorite ? <StarIconFull /> : <StarIconEmpty />}
                </div>
              </div>

              {Array.isArray(email.tags) && email.tags.length > 0 && (
                <div className="email-tags">
                  {email.tags.map((tag, index) => (
                    <Tag
                      key={`${email.mailId}-tag-${index}`}
                      name={tag.name || tag}
                    />
                  ))}
                </div>
              )}
            </div>

            <div className="email-body">
              {(email.body || "").split("\n").map((line, i) => (
                <p key={i}>{line}</p>
              ))}
            </div>

            {hasAttachments && (
              <div className="email-view-attachments">
                <h4>Attachments:</h4>
                <ul>
                  {email.attachments.map((att, index) => (
                    <li key={`att-${index}`}>
                      <span>
                        {att.name} ({(att.fileSize / 1024).toFixed(1)} KB) -{" "}
                        <i>Download link NYI</i>
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <div className="email-toolbar">
              <button className="email-toolbar-item" onClick={onClose}>
                <img src={backIcon} alt="Back" />
                <span className="small-text">Back</span>
              </button>
              <div className="flex-row-gap-30">
                <button
                  className="email-toolbar-item"
                  onClick={() => (onMarkUnread ? onMarkUnread() : null)}
                >
                  <img src={unreadIcon} alt="Mark as Unread" />
                </button>
                <button
                  className="email-toolbar-item"
                  onClick={() => (onReply ? onReply() : null)}
                >
                  <img src={replyIcon} alt="Reply" />
                </button>
                <button
                  className="email-toolbar-item"
                  onClick={() => (onForward ? onForward() : null)}
                >
                  <img src={forwardingIcon} alt="Forward" />
                </button>
              </div>
              <button
                className="email-toolbar-item trash-icon"
                onClick={() => (onDelete ? onDelete() : null)}
              >
                <img src={trashIcon} alt="Delete" />
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default EmailView;
