import React from "react";
import { useNavigate } from "react-router-dom";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import EmailView from "../../../components/EmailView/EmailView";
import "./Inbox.css";

const InboxEmailList = ({
  emails,
  setEmails,
  selectedEmail,
  setSelectedEmail,
}) => {
  const navigate = useNavigate();

  return (
    <div className="emails-container">
      {emails.map((email, index) => (
        <div
          key={index}
          onClick={(e) => {
            if (!e.target.closest(".email-star")) {
              const updatedEmails = [...emails];
              const selected = updatedEmails[index];

              if (!selected.isRead) {
                selected.isRead = true;
              }

              setEmails(updatedEmails);
              setSelectedEmail(selected);
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

      <EmailView
        email={selectedEmail}
        onClose={() => setSelectedEmail(null)}
        onDelete={(emailToDelete) => {
          setEmails((prev) => prev.filter((e) => e !== emailToDelete));
          setSelectedEmail(null);
        }}
        onMarkUnread={(emailToUpdate) => {
          const updated = emails.map((e) =>
            e === emailToUpdate ? { ...e, isRead: false } : e
          );
          setEmails(updated);
          setSelectedEmail(null);
        }}
        onReply={(emailToReply) => {
          navigate("/compose", {
            state: {
              to: emailToReply.senderEmail,
              subject: `Re: ${emailToReply.subject}`,
            },
          });
        }}
        onForward={(emailToForward) => {
          navigate("/compose", {
            state: {
              subject: `Fwd: ${emailToForward.subject}`,
              body: emailToForward.body,
            },
          });
        }}
      />
    </div>
  );
};

export default InboxEmailList;
