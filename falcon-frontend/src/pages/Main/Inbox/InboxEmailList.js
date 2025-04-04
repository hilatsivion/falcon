import React from "react";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import EmailView from "../../../components/EmailView/EmailView";
import "./Inbox.css";

const InboxEmailList = ({
  emails,
  setEmails,
  selectedEmail,
  setSelectedEmail,
}) => {
  return (
    <div className="emails-container">
      {emails.map((email, index) => (
        <div
          key={index}
          onClick={(e) => {
            if (!e.target.closest(".email-star")) {
              setSelectedEmail(email);
            }
          }}
        >
          <EmailItem
            {...email}
            onClick={() => setSelectedEmail(email)}
            onStarToggle={() => {
              const updatedEmails = [...emails];
              updatedEmails[index].isStarred = !updatedEmails[index].isStarred;
              setEmails(updatedEmails);
            }}
          />
        </div>
      ))}

      <EmailView email={selectedEmail} onClose={() => setSelectedEmail(null)} />
    </div>
  );
};

export default InboxEmailList;
