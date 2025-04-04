import React, { useState } from "react";
import "./AiComposePanel.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";
import Loader from "../../../components/Loader/Loader";

const HUGGING_FACE_API_TOKEN = process.env.REACT_APP_HUGGING_FACE_API_TOKEN;

const API_URL =
  "https://api-inference.huggingface.co/models/microsoft/Phi-3-mini-4k-instruct";

const AiComposePanel = ({ onClose, onDone }) => {
  const [idea, setIdea] = useState("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [subject, setSubject] = useState("");
  const [content, setContent] = useState("");
  const [resultReady, setResultReady] = useState(false);
  const [error, setError] = useState(null);

  const handleGenerate = async () => {
    // --- Initial Checks ---
    if (!idea.trim()) {
      setError("Please enter your email idea.");
      return;
    }
    if (!HUGGING_FACE_API_TOKEN) {
      setError(
        "API Token not found. Ensure REACT_APP_HUGGING_FACE_API_TOKEN is set in your .env file and you have restarted the server."
      );
      return;
    }
    setError(null);
    setIsGenerating(true);
    setResultReady(false);

    let generatedBody = "Error: Failed to generate body."; // Default error state
    let generatedSubject = "Error: Failed to generate subject."; // Default error state

    try {
      // --- Step 1: Generate Email Body ---
      const bodyPrompt = `Write a complete email body based on the following idea. Only output the body content, do not include a subject line yet:\n\nEmail Idea: ${idea}`;

      console.log("Requesting Body...");
      const bodyResponse = await fetch(API_URL, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${HUGGING_FACE_API_TOKEN}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          inputs: bodyPrompt,
          parameters: {
            max_new_tokens: 450, // Allocate most tokens to body
            return_full_text: false,
          },
        }),
      });

      if (!bodyResponse.ok) {
        const errorBodyText = await bodyResponse.text();
        throw new Error(
          `API Error (Body Gen - ${bodyResponse.status}): ${errorBodyText}`
        );
      }

      const bodyResult = await bodyResponse.json();
      console.log("Hugging Face Body Response:", bodyResult);

      if (
        bodyResult &&
        bodyResult[0] &&
        typeof bodyResult[0].generated_text === "string"
      ) {
        generatedBody = bodyResult[0].generated_text.trim(); // Store the generated body
        console.log("Generated Body:", generatedBody);
      } else {
        throw new Error("Unexpected response format when generating body.");
      }

      // --- Step 2: Generate Email Subject (using the generated body) ---
      const subjectPrompt = `Based on the following email idea and the generated email body, suggest a concise and relevant subject line. Output ONLY the subject line text:\n\nEmail Idea: ${idea}\n\nEmail Body:\n${generatedBody}`;

      console.log("Requesting Subject...");
      const subjectResponse = await fetch(API_URL, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${HUGGING_FACE_API_TOKEN}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          inputs: subjectPrompt,
          parameters: {
            max_new_tokens: 60, // Subject lines are short
            return_full_text: false,
          },
        }),
      });

      if (!subjectResponse.ok) {
        // We still have the body, but log subject error
        const errorSubjectText = await subjectResponse.text();
        console.error(
          `API Error (Subject Gen - ${subjectResponse.status}): ${errorSubjectText}`
        );
        generatedSubject = "Subject Error (Check Body)"; // Indicate subject gen failed
      } else {
        const subjectResult = await subjectResponse.json();
        console.log("Hugging Face Subject Response:", subjectResult);

        if (
          subjectResult &&
          subjectResult[0] &&
          typeof subjectResult[0].generated_text === "string"
        ) {
          // Clean up subject line (remove potential quotes, extra spaces/newlines)
          generatedSubject = subjectResult[0].generated_text
            .trim()
            .replace(/^["']|["']$/g, "");
          console.log("Generated Subject:", generatedSubject);
        } else {
          generatedSubject = "Subject Error (Check Body)"; // Indicate parsing failed
        }
      }

      // --- Update State with results (or errors from above) ---
      setSubject(generatedSubject);
      setContent(generatedBody);
    } catch (err) {
      // This catches errors from the Body generation mostly, or network errors
      console.error("Error during AI generation process:", err);
      setError(`Failed to generate: ${err.message}`);
      // Set subject/content to reflect the error if needed, body might be default error message
      setSubject("Generation Failed");
      setContent(generatedBody); // Body might contain initial error or partial result if subject failed later
    } finally {
      setIsGenerating(false);
      setResultReady(true); // Show results/errors
    }
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
        <p className="ai-error-message">{error}</p>

        {isGenerating && <Loader />}

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
              disabled={!idea.trim() || isGenerating}
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
