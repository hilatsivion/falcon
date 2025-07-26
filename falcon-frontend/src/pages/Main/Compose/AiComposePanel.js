import React, { useState, useEffect } from "react";
import "./AiComposePanel.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";
import Loader from "../../../components/Loader/Loader";
import { useAuth } from "../../../context/AuthContext";
import { toast } from "react-toastify";
// import { GoogleGenAI } from "@google/genai";

const AiComposePanel = ({ onClose, onDone }) => {
  const [idea, setIdea] = useState("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [subject, setSubject] = useState("");
  const [content, setContent] = useState("");
  const [resultReady, setResultReady] = useState(false);
  const [error, setError] = useState(null);
  const { aiKey } = useAuth();

  useEffect(() => {
    if (!aiKey) {
      console.warn("AI Panel: AI Key is missing from context.");
      setError(
        "AI functionality requires a valid API key. Please re-login if needed."
      );
    } else {
      setError(null); // Clear error if key becomes available
    }
  }, [aiKey]);

  const handleGenerate = async () => {
    console.log("AI Key available:", !!aiKey, "Length:", aiKey ? aiKey.length : 0, "Key preview:", aiKey ? aiKey.substring(0, 10) + "..." : "none");
    
    if (!aiKey) {
      toast.error("AI Key is missing. Cannot generate content.");
      setError("AI Key is missing. Please ensure you are logged in correctly.");
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
      // Validate API key
      if (!aiKey || aiKey.trim() === '') {
        throw new Error('Google AI API key is missing or empty');
      }

      // Make sure the API key starts with 'AIza' (Google API key format)
      if (!aiKey.startsWith('AIza')) {
        throw new Error('Invalid Google AI API key format. Key should start with "AIza"');
      }

      // Use direct REST API call instead of SDK for better browser compatibility
      const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${aiKey}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          contents: [{
            parts: [{
              text: fullPrompt
            }]
          }],
          generationConfig: {
            temperature: 0.7,
            topK: 40,
            topP: 0.95,
            maxOutputTokens: 1024,
          }
        })
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error (${response.status}): ${errorText}`);
      }

      const result = await response.json();

      // --- Parse the result ---
      console.log("Gemini AI Response:", result);

      let generatedSubject = "Error: Could not parse subject";
      let generatedBody = "Error: Could not parse body";

      if (result && result.candidates && result.candidates[0] && result.candidates[0].content && result.candidates[0].content.parts && result.candidates[0].content.parts[0]) {
        const rawText = result.candidates[0].content.parts[0].text;

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
          "Error: Unexpected response format from AI (expected candidates array with content).";
      }

      setSubject(generatedSubject);
      setContent(generatedBody);
    } catch (err) {
      console.error("Error calling Gemini AI API:", err);
      console.error("Error details:", {
        name: err.name,
        message: err.message,
        stack: err.stack
      });
      
      // Provide more specific error messages based on error type
      let errorMessage = `Failed to generate: ${err.message}`;
      
      if (err.message.includes("API Key must be set")) {
        errorMessage = "Google AI API key is not properly configured. Please check your login credentials.";
      } else if (err.message.includes("Invalid Google AI API key format")) {
        errorMessage = "The API key format is invalid. Please contact support.";
      } else if (err.message.includes("network") || err.message.includes("fetch")) {
        errorMessage = "Network error. Please check your internet connection and try again.";
      }
      
      setError(errorMessage);
      
      // Provide fallback content for testing purposes
      setSubject("AI Service Temporarily Unavailable");
      setContent("The AI service is currently experiencing issues. Please try again later or compose your email manually.");
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
