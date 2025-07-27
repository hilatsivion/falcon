import React, { useState } from "react";
import ConfirmPopup from "../Popup/ConfirmPopup";
import { ReactComponent as ForwardingIcon } from "../../assets/icons/black/forward-icon.svg";
import { ReactComponent as ReplyIcon } from "../../assets/icons/black/reply-icon.svg";
import { ReactComponent as UnreadIcon } from "../../assets/icons/black/unread-icon.svg";
import { ReactComponent as TrashIcon } from "../../assets/icons/black/trash-red-icon.svg";
import { ReactComponent as BackIcon } from "../../assets/icons/black/arrow-left-20.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { ReactComponent as CopyIcon } from "../../assets/icons/black/copy_to_clipboard.svg";
import { Tag } from "../Tag/Tag";
import "./EmailView.css";
import { parseSender } from "../../utils/emailUtils";
import { formatEmailTime } from "../../utils/formatters";

import { CopyToClipboard } from "react-copy-to-clipboard";
import { toast } from "react-toastify";

const EmailView = ({
  email,
  onClose,
  onDelete,
  onMarkUnread,
  onToggleFavorite,
  onReply,
  onForward,
  isTrashPage,
}) => {
  const [showDeletePopup, setShowDeletePopup] = useState(false);

  if (!email) {
    return null;
  }

  const { name: displayName, email: senderEmail } = parseSender(email?.sender);

  const handleStarClick = (e) => {
    e.stopPropagation(); // Prevents closing the popup when clicking the star
    if (onToggleFavorite) {
      onToggleFavorite();
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
    return recipients.map((r) => r.email || r)[0] || ""; // only one recipient for now
  };

  const formattedTime = formatEmailTime(email?.timeReceived);

  const hasAttachments =
    Array.isArray(email.attachments) && email.attachments.length > 0;

  return (
    <>
      <ConfirmPopup
        isOpen={showDeletePopup}
        message="Are you sure you want to delete?"
        confirmText="Delete"
        cancelText="Cancel"
        onConfirm={() => {
          onDelete?.();
          setShowDeletePopup(false);
        }}
        onCancel={() => setShowDeletePopup(false)}
      />

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
                <div className="email-view-top-section">
                  <div className="email-view-sender-recipient">
                    <div className="email-sender-container">
                      <div className="flex">
                        <div
                          className="email-avatar"
                          style={{ backgroundColor: avatarColorCalculated }}
                        >
                          {senderInitial}
                        </div>
                        <span className="email-sender">
                          {displayName || senderEmail}
                        </span>{" "}
                      </div>
                      <div className="email-view-time-action">
                        <span className="email-time">{formattedTime}</span>
                      </div>
                    </div>
                    <div className="email-recipient-line">
                      <span className="email-recipients">
                        To: {formatRecipients(email.recipients)}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="email-subject-line">
                  <h3 className="email-subject">
                    {email.subject || "(No Subject)"}
                  </h3>

                  {!isTrashPage && (
                    <div className="email-star" onClick={handleStarClick}>
                      {email.isFavorite ? <StarIconFull /> : <StarIconEmpty />}
                    </div>
                  )}
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
                <CopyToClipboard
                  text={email.body || ""}
                  onCopy={() => toast.success("Copied to clipboard!")}
                >
                  <div className="email-copy-btn">
                    <CopyIcon />
                  </div>
                </CopyToClipboard>

                <div dangerouslySetInnerHTML={{ __html: email.body || "" }} />
              </div>

              {hasAttachments && (
                <div className="email-view-attachments">
                  <h4>Attachments:</h4>
                  <ul>
                    {email.attachments.map((att, index) => {
                      // Convert the file path to a URL for the frontend
                      const getAttachmentUrl = (filePath) => {
                        if (!filePath) return null;
                        
                        // Remove the "Storage/" prefix and add "/attachments/" prefix
                        const relativePath = filePath.replace(/^Storage\//, '');
                        return `/attachments/${relativePath}`;
                      };

                      const attachmentUrl = getAttachmentUrl(att.filePath);
                      const isImage = att.name && /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(att.name);
                      
                                             return (
                         <li key={`att-${index}`}>
                           <div className="attachment-item">
                             <div className="attachment-info">
                               {isImage && attachmentUrl && (
                                 <div className="attachment-thumbnail">
                                   <img 
                                     src={attachmentUrl} 
                                     alt={att.name}
                                     className="attachment-image"
                                   />
                                 </div>
                               )}
                               <div className="attachment-details">
                                 <span className="attachment-name">
                                   {att.name}
                                 </span>
                                 <span className="attachment-size">
                                   ({(att.fileSize / 1024).toFixed(1)} KB)
                                 </span>
                               </div>
                             </div>
                             <div className="attachment-actions">
                               {attachmentUrl && (
                                 <a 
                                   href={attachmentUrl} 
                                   download={att.name}
                                   className="attachment-download-link"
                                 >
                                   Download
                                 </a>
                               )}
                             </div>
                           </div>
                         </li>
                       );
                    })}
                  </ul>
                </div>
              )}

              <div className="email-toolbar">
                <button className="email-toolbar-item" onClick={onClose}>
                  <BackIcon />
                  <span className="small-text">Back</span>
                </button>
                <div className="flex-row-gap-30">
                  <button
                    className="email-toolbar-item"
                    onClick={() => (onMarkUnread ? onMarkUnread() : null)}
                  >
                    <UnreadIcon />
                  </button>
                  <button
                    className="email-toolbar-item"
                    onClick={() => (onReply ? onReply() : null)}
                  >
                    <ReplyIcon />
                  </button>
                  <button
                    className="email-toolbar-item"
                    onClick={() => (onForward ? onForward() : null)}
                  >
                    <ForwardingIcon />
                  </button>
                </div>
                <button
                  className="email-toolbar-item trash-icon"
                  onClick={() => setShowDeletePopup(true)}
                >
                  <TrashIcon />
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </>
  );
};

export default EmailView;
