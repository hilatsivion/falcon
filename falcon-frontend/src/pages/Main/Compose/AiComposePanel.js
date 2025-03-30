import React, { useState } from "react";
import "./AiComposePanel.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";
import Loader from "../../../components/Loader/Loader";

const AiComposePanel = ({ onClose, onDone }) => {
  const [idea, setIdea] = useState("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [subject, setSubject] = useState("");
  const [content, setContent] = useState("");
  const [resultReady, setResultReady] = useState(false);

  const handleGenerate = () => {
    if (!idea.trim()) return;
    setIsGenerating(true);

    // Simulate API call
    setTimeout(() => {
      setIsGenerating(false);
      setSubject("The subject of the email the AI gen..");
      setContent(`Dear Shlomi,

I wanted to ask you if you can meet tomorrow morning at 09:00.

Let me know soon.

Thanks,
Moshe`);
      setResultReady(true);
    }, 2000);
  };

  const handleTryAgain = () => {
    setResultReady(false);
    setSubject("");
    setContent("");
  };

  const handleDone = () => {
    onDone({ subject, content });
    onClose();
  };

  return (
    <>
      {isGenerating && <Loader />}
      <div className="ai-compose-panel">
        <div className="ai-header">
          <h2>
            <span className="gradient-text">Compose with AI</span>
          </h2>
          <button className="close-btn" onClick={onClose}>
            <CloseIcon />
          </button>
        </div>

        {!resultReady ? (
          <>
            <p className="subtext">
              Write your email idea, and the AI will craft the content and
              subject for you.
            </p>
            <textarea
              className="ai-textarea"
              placeholder="Write here..."
              value={idea}
              onChange={(e) => setIdea(e.target.value)}
            />
            <button
              className="generate-btn"
              onClick={handleGenerate}
              disabled={!idea.trim()}
            >
              Generate
            </button>
          </>
        ) : (
          <>
            <p className="subtext">
              The results! You can edit this, or try again if you like.
            </p>

            <label className="ai-label">Generated Subject:</label>
            <input
              className="ai-textarea ai-subject"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
            />

            <label className="ai-label">Generated Content:</label>
            <textarea
              className="ai-textarea ai-body"
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />

            <button className="btn-border-blue done" onClick={handleDone}>
              Done
            </button>
            <p className="retry-btn" onClick={handleTryAgain}>
              Try generate again
            </p>
          </>
        )}
      </div>
    </>
  );
};

export default AiComposePanel;
