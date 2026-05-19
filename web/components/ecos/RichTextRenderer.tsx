"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useEffect, useId, useRef, useState } from "react";
import ReactMarkdown, { type Components } from "react-markdown";
import rehypeKatex from "rehype-katex";
import remarkGfm from "remark-gfm";
import remarkMath from "remark-math";

export type RichTextRendererProps = {
  /** Markdown source to render. */
  value: string;
};

let isMermaidInitialized = false;

/**
 * Renders trusted EngiFlow markdown features without enabling raw HTML.
 *
 * @param props - Rich text renderer options.
 * @param props.value - Markdown source to render.
 * @returns Rendered markdown with GFM, KaTeX, and Mermaid fenced blocks.
 */
export default function RichTextRenderer({ value }: RichTextRendererProps) {
  return (
    <Box
      className="rich-text-renderer"
      sx={{
        "& table": {
          borderCollapse: "collapse",
          width: "100%",
          mb: 2,
          mt: 1,
          display: "block",
          overflowX: "auto",
        },
        "& th, & td": {
          border: 1,
          borderColor: "divider",
          p: 1.5,
          textAlign: "left",
          minWidth: 120,
        },
        "& th": {
          bgcolor: "action.hover",
          fontWeight: 600,
          color: "text.primary",
        },
        "& td": {
          color: "text.secondary",
          verticalAlign: "top",
        },
        "& pre": {
          bgcolor: "action.hover",
          p: 2,
          borderRadius: 1,
          overflowX: "auto",
          border: 1,
          borderColor: "divider",
          my: 2,
        },
        "& code": {
          fontFamily: "monospace",
          fontSize: "0.875rem",
          bgcolor: "action.hover",
          px: 0.75,
          py: 0.25,
          borderRadius: 0.5,
          color: "text.primary",
        },
        "& pre > code": {
          bgcolor: "transparent",
          p: 0,
          px: 0,
          py: 0,
          borderRadius: 0,
          display: "block",
          fontSize: "0.8125rem",
        },
        "& .katex-display": {
          my: 2,
          p: 1.5,
          overflowX: "auto",
          overflowY: "hidden",
        },
        "& p": {
          mb: 1.5,
          "&:last-child": { mb: 0 },
        },
        "& ul, & ol": {
          mb: 1.5,
          pl: 3,
        },
        "& li": {
          mb: 0.5,
        },
      }}
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkMath]}
        rehypePlugins={[rehypeKatex]}
        skipHtml
        components={markdownComponents}
      >
        {value}
      </ReactMarkdown>
    </Box>
  );
}

const markdownComponents: Components = {
  a({ children, href, ...props }) {
    const safeHref = normalizeSafeHref(href);

    if (!safeHref) {
      return <>{children}</>;
    }

    return (
      <a
        {...props}
        href={safeHref}
        rel="noopener noreferrer"
        target={safeHref.startsWith("#") ? undefined : "_blank"}
      >
        {children}
      </a>
    );
  },
  code({ children, className, ...props }) {
    const language = readCodeLanguage(className);
    const code = String(children).replace(/\n$/, "");

    if (language === "mermaid") {
      return <MermaidDiagram definition={code} />;
    }

    return (
      <code {...props} className={className}>
        {children}
      </code>
    );
  },
};

type MermaidDiagramProps = {
  definition: string;
};

/**
 * Renders one Mermaid diagram with cancellation and DOM cleanup safeguards.
 *
 * @param props - Mermaid diagram rendering options.
 * @param props.definition - Mermaid diagram source.
 * @returns Diagram container or a validation error.
 */
function MermaidDiagram({ definition }: MermaidDiagramProps) {
  const reactId = useId();
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isRendering, setIsRendering] = useState(true);

  useEffect(() => {
    let isCanceled = false;
    const renderedContainer = containerRef.current;
    const diagramId = `mermaid-${reactId.replace(/[^a-zA-Z0-9_-]/g, "")}`;

    async function renderDiagram(): Promise<void> {
      setIsRendering(true);
      setErrorMessage(null);

      try {
        const mermaidModule = await import("mermaid");
        const mermaid = mermaidModule.default;

        if (!isMermaidInitialized) {
          mermaid.initialize({
            startOnLoad: false,
            securityLevel: "strict",
            htmlLabels: false,
            flowchart: {
              useMaxWidth: true,
              htmlLabels: false,
            },
          });
          isMermaidInitialized = true;
        }

        const { svg } = await mermaid.render(diagramId, definition);

        if (!isCanceled && containerRef.current) {
          containerRef.current.innerHTML = svg;
        }
      } catch (error) {
        if (!isCanceled) {
          setErrorMessage(getMermaidErrorMessage(error));
        }
      } finally {
        if (!isCanceled) {
          setIsRendering(false);
        }
      }
    }

    void renderDiagram();

    return () => {
      isCanceled = true;
      if (renderedContainer) {
        renderedContainer.replaceChildren();
      }
    };
  }, [definition, reactId]);

  if (errorMessage) {
    return (
      <Alert severity="warning" variant="outlined" sx={{ my: 1 }}>
        {errorMessage}
      </Alert>
    );
  }

  return (
    <Stack
      className="mermaid-diagram"
      spacing={1}
      sx={{
        alignItems: "center",
        justifyContent: "center",
        minHeight: isRendering ? 96 : undefined,
      }}
    >
      {isRendering ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <CircularProgress size={18} />
          <Typography variant="caption" color="text.secondary">
            Rendering diagram
          </Typography>
        </Stack>
      ) : null}
      <Box ref={containerRef} sx={{ width: "100%", overflowX: "auto" }} />
    </Stack>
  );
}

function readCodeLanguage(className: string | undefined): string | null {
  const match = /language-([\w-]+)/.exec(className ?? "");

  return match?.[1]?.toLowerCase() ?? null;
}

function normalizeSafeHref(href: string | undefined): string | null {
  if (!href) {
    return null;
  }

  const trimmedHref = href.trim();
  if (
    trimmedHref.startsWith("#") ||
    trimmedHref.startsWith("/") ||
    /^https?:\/\//i.test(trimmedHref) ||
    /^mailto:/i.test(trimmedHref)
  ) {
    return trimmedHref;
  }

  return null;
}

function getMermaidErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message.trim().length > 0) {
    return `Unable to render Mermaid diagram: ${error.message}`;
  }

  return "Unable to render Mermaid diagram.";
}
