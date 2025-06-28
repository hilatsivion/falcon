import React from "react";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import "./Inbox.css";

const InboxEmailList = ({
  emails,
  onEmailSelect,
  onToggleFavorite,
  isTrashPage,
}) => {
  return (
    <div className="emails-container">
      {emails.map((email) => (
        <EmailItem
          key={email.mailId}
          mailId={email.mailId}
          sender={email.sender}
          subject={email.subject}
          bodySnippet={email.bodySnippet}
          timeReceived={email.timeReceived}
          isRead={email.isRead}
          isFavorite={email.isFavorite}
          tags={email.tags}
          onClick={onEmailSelect}
          onStarToggle={onToggleFavorite}
          isTrashPage={isTrashPage}
        />
      ))}
    </div>
  );
};

export default InboxEmailList;
