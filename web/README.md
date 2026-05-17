# EngiFlow Web

Next.js 16 App Router frontend for EngiFlow.

## ECO PR-Like Experience

- `/ecos` uses MUI X Community DataGrid with server pagination, global search, status and priority filters, created-date range filters, and user-context filters.
- `/ecos/[id]` renders a GitHub PR-style ECO shell with sticky workflow actions, reviewer quorum state, Conversation, Affected Items, and Files tabs.
- Conversation comments support GitHub-flavored Markdown, tables, links, KaTeX math, and Mermaid diagrams through `RichTextRenderer`. Raw HTML is not enabled.
- Comment drafts autosave to `localStorage` as `eco-{id}-draft` and clear only after a successful comment submission.
- SignalR updates connect directly to `/hubs/ecos` using `NEXT_PUBLIC_API_URL` or `NEXT_PUBLIC_API_BASE_URL`, falling back to `http://localhost:8080`.
- Attachment cards open a right-side Drawer and use the API download endpoint to retrieve short-lived S3/MinIO pre-signed URLs.

## Local Commands

```bash
npm install
npm run dev
npm run lint
npm run build
```

The web API proxy uses same-origin `/api/...` requests by default. Docker Compose sets `API_INTERNAL_BASE_URL=http://api:8080`; local development without Docker falls back to `http://localhost:8080`.
