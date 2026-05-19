"use client";

import Box from "@mui/material/Box";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import { useState } from "react";
import RichTextRenderer from "./RichTextRenderer";

export type EcoComposerProps = {
  /** The markdown value to edit. */
  value: string;
  /** Callback when the markdown value changes. */
  onChange: (value: string) => void;
  /** Whether the editor is in a readonly/disabled state. */
  disabled?: boolean;
  /** Maximum character limit for the description. */
  maxLength?: number;
  /** Optional error message to display. */
  error?: string;
};

/**
 * A GitHub-inspired markdown authoring component with Write and Preview tabs.
 * Designed for high-density technical engineering change descriptions.
 *
 * @param props - Eco composer options.
 * @returns A tabbed markdown editor and previewer.
 */
export default function EcoComposer({
  value,
  onChange,
  disabled = false,
  maxLength = 4000,
  error,
}: EcoComposerProps) {
  const [activeTab, setActiveTab] = useState(0);

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  return (
    <Box
      sx={{
        width: "100%",
        border: 1,
        borderColor: error ? "error.main" : "divider",
        borderRadius: 1,
        overflow: "hidden",
        bgcolor: "background.paper",
      }}
    >
      <Box sx={{ borderBottom: 1, borderColor: "divider", bgcolor: "background.paper" }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="Markdown editor tabs"
          sx={{
            minHeight: 40,
            "& .MuiTab-root": {
              textTransform: "none",
              minHeight: 40,
              fontWeight: 500,
              fontSize: "0.875rem",
            },
          }}
        >
          <Tab label="Write" id="composer-tab-0" aria-controls="composer-panel-0" />
          <Tab label="Preview" id="composer-tab-1" aria-controls="composer-panel-1" />
        </Tabs>
      </Box>

      <Box
        role="tabpanel"
        hidden={activeTab !== 0}
        id="composer-panel-0"
        aria-labelledby="composer-tab-0"
        sx={{ p: 0 }}
      >
        <TextField
          multiline
          minRows={10}
          fullWidth
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
          placeholder="Describe the engineering change, justification, and impact..."
          slotProps={{
            htmlInput: {
              maxLength,
              style: {
                fontFamily: "var(--font-roboto-mono), monospace",
                fontSize: "0.875rem",
              },
            },
          }}
          sx={{
            "& .MuiOutlinedInput-root": {
              "& fieldset": { border: "none" },
            },
            "& .MuiInputBase-input": {
              p: 2,
            },
          }}
        />
      </Box>

      <Box
        role="tabpanel"
        hidden={activeTab !== 1}
        id="composer-panel-1"
        aria-labelledby="composer-tab-1"
        sx={{
          p: 2,
          minHeight: 250,
          bgcolor: "background.paper",
          maxHeight: 600,
          overflowY: "auto",
        }}
      >
        {value.trim() ? (
          <RichTextRenderer value={value} />
        ) : (
          <Box
            sx={{
              height: "100%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "text.secondary",
              fontStyle: "italic",
            }}
          >
            Nothing to preview
          </Box>
        )}
      </Box>

      {error || maxLength ? (
        <Box
          sx={{
            px: 2,
            py: 0.75,
            borderTop: 1,
            borderColor: "divider",
            display: "flex",
            justifyContent: "space-between",
            bgcolor: "background.paper",
          }}
        >
          <Box sx={{ color: error ? "error.main" : "text.secondary", fontSize: "0.75rem" }}>
            {error || ""}
          </Box>
          <Box sx={{ color: "text.secondary", fontSize: "0.75rem" }}>
            {value.length}/{maxLength}
          </Box>
        </Box>
      ) : null}
    </Box>
  );
}
