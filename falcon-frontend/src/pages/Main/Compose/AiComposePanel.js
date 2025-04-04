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
    if (
      !idea.trim() ||
      !HUGGING_FACE_API_TOKEN ||
      HUGGING_FACE_API_TOKEN === "hf_YOUR_TOKEN_HERE"
    ) {
      setError(
        "Please enter your idea and ensure your Hugging Face API Token is set in AiComposePanel.js"
      );
      return;
    }
    setError(null); // Clear previous errors
    setIsGenerating(true);
    setResultReady(false); // Clear previous results visually

    // --- Construct the prompt ---
    const fullPrompt = `Based on the following idea, generate an email subject and body.
Output the result ONLY as a valid JSON object like this: {"subject": "Generated Subject", "body": "Generated Body Content"}

Email Idea: ${idea}`;

    try {
      const response = await fetch(API_URL, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${HUGGING_FACE_API_TOKEN}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          inputs: fullPrompt,
          parameters: {
            max_new_tokens: 512,
            return_full_text: false,
          },
        }),
      });

      if (!response.ok) {
        const errorBody = await response.text();
        throw new Error(`API Error (${response.status}): ${errorBody}`);
      }

      const result = await response.json();

      // --- Parse the result ---
      console.log("Hugging Face API Response:", result);

      let generatedSubject = "Error: Could not parse subject";
      let generatedBody = "Error: Could not parse body";

      if (result && result[0] && typeof result[0].generated_text === "string") {
        const rawText = result[0].generated_text;

        // --- Attempt to extract JSON from the raw text ---
        try {
          const jsonStart = rawText.indexOf("{"); // Find the first '{'
          const jsonEnd = rawText.lastIndexOf("}") + 1; // Find the last '}'

          if (jsonStart !== -1 && jsonEnd !== 0 && jsonEnd > jsonStart) {
            const jsonString = rawText.substring(jsonStart, jsonEnd);
            const generatedOutput = JSON.parse(jsonString);

            if (generatedOutput.subject && generatedOutput.body) {
              generatedSubject = generatedOutput.subject;
              generatedBody = generatedOutput.body;
            } else {
              generatedSubject = "Parsing Error";
              generatedBody =
                "AI response JSON parsed, but missing subject or body keys.";
            }
          } else {
            // Couldn't find '{' and '}' reliably
            generatedSubject = "Parsing Error";
            generatedBody =
              "Could not find JSON structure within the AI response text.";
          }
        } catch (parseError) {
          console.error("Failed to parse extracted JSON:", parseError);

          generatedSubject = "Subject (AI Generated - Check Body)";
          generatedBody = rawText;
        }
      } else {
        generatedSubject = "Response Error";
        generatedBody =
          "Error: Unexpected response format from AI (expected array with generated_text string).";
      }

      setSubject(generatedSubject);
      setContent(generatedBody);
    } catch (err) {
      console.error("Error calling Hugging Face API:", err);
      setError(`Failed to generate: ${err.message}`);
    } finally {
      setIsGenerating(false);
      setResultReady(true);
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
