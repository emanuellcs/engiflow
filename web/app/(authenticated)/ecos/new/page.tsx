"use client";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import SaveIcon from "@mui/icons-material/Save";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Breadcrumbs from "@mui/material/Breadcrumbs";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import Grid from "@mui/material/Grid";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { type FormEvent, useEffect, useState } from "react";
import EcoComposer from "@/components/ecos/EcoComposer";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import type {
  CreateEcoPriority,
  CreateEcoRequest,
  EcoDetailsDto,
} from "@/lib/types/eco";

type CreateEcoFormState = {
  title: string;
  description: string;
  priority: CreateEcoPriority;
};

type CreateEcoFieldErrors = Partial<Record<keyof CreateEcoFormState, string>>;

const createEcoPriorityOptions: CreateEcoPriority[] = ["Low", "Medium", "High"];
const initialFormState: CreateEcoFormState = {
  title: "",
  description: "",
  priority: "Medium",
};
const titleMaxLength = 200;
const descriptionMaxLength = 4000;
const defaultCreateError =
  "Unable to create the Engineering Change Order. Review the details and try again.";
const requesterRole = "Requester";

/**
 * Renders the authenticated new ECO creation route.
 *
 * @returns The dense ECO creation form page.
 */
export default function NewEcoPage() {
  const router = useRouter();
  const { user } = useAuth();
  const storageKey = user ? `new-eco-draft-${user.id}` : null;

  const [form, setForm] = useState<CreateEcoFormState>(() => {
    if (typeof window !== "undefined" && storageKey) {
      const saved = localStorage.getItem(storageKey);
      if (saved) {
        try {
          return { ...initialFormState, ...JSON.parse(saved) };
        } catch (e) {
          console.error("Failed to parse ECO draft", e);
        }
      }
    }
    return initialFormState;
  });

  const [fieldErrors, setFieldErrors] = useState<CreateEcoFieldErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDiscardDialogOpen, setIsDiscardDialogOpen] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  const canCreateEco = isAdminOrOwner(user?.role) || user?.role === requesterRole;
  const isDirty =
    form.title !== initialFormState.title ||
    form.description !== initialFormState.description ||
    form.priority !== initialFormState.priority;

  // Debounced Auto-save
  useEffect(() => {
    if (!storageKey || !isDirty) return;

    const timeoutId = setTimeout(() => {
      localStorage.setItem(storageKey, JSON.stringify(form));
      setLastSaved(new Date());
    }, 2000);

    return () => clearTimeout(timeoutId);
  }, [form, storageKey, isDirty]);

  /**
   * Validates and submits the ECO draft to the API.
   *
   * @param event - Form submit event from the creation form.
   * @returns A promise that resolves after submission completes.
   */
  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const nextErrors = validateCreateEcoForm(form);

    if (hasFieldErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      // Scroll to top to see error alerts
      window.scrollTo({ top: 0, behavior: "smooth" });
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const request: CreateEcoRequest = {
        title: form.title.trim(),
        description: form.description.trim(),
        priority: form.priority,
      };
      const createdEco = await apiFetch<EcoDetailsDto>("/api/ecos", {
        method: "POST",
        body: request,
      });

      // Clear draft on success
      if (storageKey) {
        localStorage.removeItem(storageKey);
      }

      const createdId = readCreatedEcoId(createdEco);
      router.push(createdId ? `/ecos/${createdId}` : "/ecos");
    } catch (error) {
      setErrorMessage(getCreateEcoErrorMessage(error));
      window.scrollTo({ top: 0, behavior: "smooth" });
    } finally {
      setIsSubmitting(false);
    }
  }

  /**
   * Updates a form field and clears its current validation message.
   *
   * @param field - Form field to update.
   * @param value - New field value.
   * @returns Nothing.
   */
  function handleFieldChange(
    field: keyof CreateEcoFormState,
    value: string | CreateEcoPriority,
  ): void {
    setForm((current) => ({ ...current, [field]: value }));
    clearFieldError(field);
  }

  /**
   * Clears one field-level validation message after user edits.
   *
   * @param field - Form field whose validation message should be cleared.
   * @returns Nothing.
   */
  function clearFieldError(field: keyof CreateEcoFormState): void {
    setFieldErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  const handleCancelClick = () => {
    if (isDirty) {
      setIsDiscardDialogOpen(true);
    } else {
      router.push("/ecos");
    }
  };

  const handleConfirmDiscard = () => {
    if (storageKey) {
      localStorage.removeItem(storageKey);
    }
    router.push("/ecos");
  };

  if (!canCreateEco) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Stack spacing={3}>
          <Breadcrumbs aria-label="breadcrumb">
            <Link underline="hover" color="inherit" component={NextLink} href="/ecos">
              ECOs
            </Link>
            <Typography color="text.primary">New ECO</Typography>
          </Breadcrumbs>
          <Alert severity="warning">
            Requester or Administrator access is required to create ECOs.
          </Alert>
          <Button
            component={NextLink}
            href="/ecos"
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            sx={{ alignSelf: "flex-start", textTransform: "none" }}
          >
            Back to ECOs
          </Button>
        </Stack>
      </Container>
    );
  }

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <Container maxWidth="md" sx={{ pt: 3, pb: 10 }}>
        <Stack spacing={4}>
          <Breadcrumbs aria-label="breadcrumb">
            <Link underline="hover" color="inherit" component={NextLink} href="/ecos">
              ECOs
            </Link>
            <Typography color="text.primary">New Engineering Change Order</Typography>
          </Breadcrumbs>

          <Box component="form" id="create-eco-form" noValidate onSubmit={handleSubmit}>
            <Stack spacing={4}>
              {errorMessage ? (
                <Alert severity="error" variant="filled">
                  {errorMessage}
                </Alert>
              ) : null}

              <Stack spacing={1}>
                <Typography variant="h4" component="h1" sx={{ fontWeight: 700 }}>
                  Create New ECO
                </Typography>
                <Typography variant="body1" color="text.secondary">
                  Author a technical change request. Use the description to provide
                  detailed justification and impact analysis.
                </Typography>
              </Stack>

              <Grid container spacing={3}>
                <Grid size={12}>
                  <TextField
                    id="title"
                    name="title"
                    placeholder="Engineering Change Order Title"
                    value={form.title}
                    onChange={(event) =>
                      handleFieldChange("title", event.target.value)
                    }
                    disabled={isSubmitting}
                    error={Boolean(fieldErrors.title)}
                    helperText={fieldErrors.title}
                    autoFocus
                    required
                    fullWidth
                    slotProps={{
                      htmlInput: {
                        maxLength: titleMaxLength,
                        style: {
                          fontSize: "1.5rem",
                          fontWeight: 600,
                          padding: "12px 16px",
                        },
                      },
                    }}
                    sx={{
                      "& .MuiOutlinedInput-root": {
                        bgcolor: "background.paper",
                      },
                    }}
                  />
                </Grid>

                <Grid size={12}>
                  <Stack spacing={1.5}>
                    <Typography
                      variant="subtitle2"
                      color="text.secondary"
                      sx={{ fontWeight: 600 }}
                    >
                      Priority Level
                    </Typography>
                    <ToggleButtonGroup
                      value={form.priority}
                      exclusive
                      onChange={(_, value) => {
                        if (value) handleFieldChange("priority", value);
                      }}
                      disabled={isSubmitting}
                      fullWidth
                      size="small"
                      sx={{
                        "& .MuiToggleButton-root": {
                          flex: 1,
                          textTransform: "none",
                          fontWeight: 600,
                          transition: "all 0.2s ease-in-out",
                        },
                      }}
                    >
                      <ToggleButton
                        value="Low"
                        sx={{
                          borderColor: "success.main",
                          color: "success.main",
                          "&.Mui-selected": {
                            bgcolor: "success.main",
                            color: "white",
                            "&:hover": { bgcolor: "success.dark" },
                          },
                        }}
                      >
                        Low
                      </ToggleButton>
                      <ToggleButton
                        value="Medium"
                        sx={{
                          borderColor: "info.main",
                          color: "info.main",
                          "&.Mui-selected": {
                            bgcolor: "info.main",
                            color: "white",
                            "&:hover": { bgcolor: "info.dark" },
                          },
                        }}
                      >
                        Medium
                      </ToggleButton>
                      <ToggleButton
                        value="High"
                        sx={{
                          borderColor: "warning.main",
                          color: "warning.main",
                          "&.Mui-selected": {
                            bgcolor: "warning.main",
                            color: "white",
                            "&:hover": { bgcolor: "warning.dark" },
                          },
                        }}
                      >
                        High
                      </ToggleButton>
                    </ToggleButtonGroup>
                    {fieldErrors.priority && (
                      <Typography variant="caption" color="error">
                        {fieldErrors.priority}
                      </Typography>
                    )}
                  </Stack>
                </Grid>

                <Grid size={12}>
                  <Stack spacing={1.5}>
                    <Typography
                      variant="subtitle2"
                      color="text.secondary"
                      sx={{ fontWeight: 600 }}
                    >
                      Change Description
                    </Typography>
                    <EcoComposer
                      value={form.description}
                      onChange={(value) => handleFieldChange("description", value)}
                      disabled={isSubmitting}
                      maxLength={descriptionMaxLength}
                      error={fieldErrors.description}
                    />
                  </Stack>
                </Grid>
              </Grid>

              {/* Action Area (Normal Flow) */}
              <Stack
                direction="row"
                sx={{
                  justifyContent: "space-between",
                  alignItems: "center",
                  pt: 2,
                }}
              >
                <Box>
                  {lastSaved && (
                    <Typography variant="caption" color="text.secondary">
                      Draft auto-saved at {lastSaved.toLocaleTimeString()}
                    </Typography>
                  )}
                </Box>
                <Stack direction="row" spacing={2}>
                  <Button
                    variant="outlined"
                    onClick={handleCancelClick}
                    disabled={isSubmitting}
                    sx={{
                      textTransform: "none",
                      minWidth: 100,
                    }}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    variant="contained"
                    disabled={isSubmitting}
                    startIcon={
                      isSubmitting ? undefined : <SaveIcon fontSize="small" />
                    }
                    sx={{
                      minHeight: 40,
                      minWidth: 140,
                      textTransform: "none",
                      fontWeight: 600,
                    }}
                  >
                    {isSubmitting ? (
                      <CircularProgress color="inherit" size={20} thickness={5} />
                    ) : (
                      "Create ECO"
                    )}
                  </Button>
                </Stack>
              </Stack>
            </Stack>
          </Box>
        </Stack>
      </Container>

      {/* Discard Changes Dialog */}
      <Dialog
        open={isDiscardDialogOpen}
        onClose={() => setIsDiscardDialogOpen(false)}
        aria-labelledby="discard-dialog-title"
      >
        <DialogTitle id="discard-dialog-title">Discard unsaved changes?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            You have unsaved changes in your ECO draft. Are you sure you want to
            cancel? This will also clear your local draft.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ p: 2, pt: 0 }}>
          <Button onClick={() => setIsDiscardDialogOpen(false)} sx={{ textTransform: "none" }}>
            Keep Editing
          </Button>
          <Button
            onClick={handleConfirmDiscard}
            color="error"
            variant="contained"
            sx={{ textTransform: "none" }}
          >
            Discard Changes
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

/**
 * Validates user-entered ECO creation fields before the API call.
 *
 * @param form - Current creation form state.
 * @returns Field-level validation messages keyed by form field.
 */
function validateCreateEcoForm(
  form: CreateEcoFormState,
): CreateEcoFieldErrors {
  const errors: CreateEcoFieldErrors = {};

  if (!form.title.trim()) {
    errors.title = "Title is required.";
  } else if (form.title.trim().length > titleMaxLength) {
    errors.title = `Title cannot exceed ${titleMaxLength} characters.`;
  }

  if (!form.description.trim()) {
    errors.description = "Description is required.";
  } else if (form.description.trim().length > descriptionMaxLength) {
    errors.description = `Description cannot exceed ${descriptionMaxLength} characters.`;
  }

  if (!createEcoPriorityOptions.includes(form.priority)) {
    errors.priority = "Select a valid priority.";
  }

  return errors;
}

/**
 * Checks whether any field-level validation messages are present.
 *
 * @param errors - Field errors returned by form validation.
 * @returns True when at least one field has a validation message.
 */
function hasFieldErrors(errors: CreateEcoFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

/**
 * Reads the created ECO identifier from a create response.
 *
 * @param eco - Detailed ECO response returned by the API.
 * @returns A trimmed ECO identifier when available, otherwise null.
 */
function readCreatedEcoId(eco: EcoDetailsDto | null | undefined): string | null {
  if (!eco || typeof eco.id !== "string" || !eco.id.trim()) {
    return null;
  }

  return eco.id.trim();
}

/**
 * Produces the user-facing create error message from API and network failures.
 *
 * @param error - Unknown error thrown while creating an ECO.
 * @returns A stable error message suitable for a Material UI Alert.
 */
function getCreateEcoErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? defaultCreateError;
  }

  return defaultCreateError;
}

/**
 * Reads the most useful RFC 7807 or validation message from an API error body.
 *
 * @param details - Unknown API error details payload.
 * @returns A user-facing message when one is available, otherwise null.
 */
function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
  }

  const validationMessage = readValidationMessage(details);

  if (validationMessage) {
    return validationMessage;
  }

  if (
    "detail" in details &&
    typeof details.detail === "string" &&
    details.detail.trim().length > 0
  ) {
    return details.detail;
  }

  if (
    "title" in details &&
    typeof details.title === "string" &&
    details.title.trim().length > 0
  ) {
    return details.title;
  }

  return null;
}

/**
 * Reads the first validation message from a ProblemDetails errors dictionary.
 *
 * @param details - ProblemDetails-like API error object.
 * @returns First validation message when present, otherwise null.
 */
function readValidationMessage(details: object): string | null {
  if (
    !("errors" in details) ||
    !details.errors ||
    typeof details.errors !== "object"
  ) {
    return null;
  }

  for (const messages of Object.values(details.errors)) {
    if (!Array.isArray(messages)) {
      continue;
    }

    const message = messages.find(
      (item): item is string => typeof item === "string" && item.length > 0,
    );

    if (message) {
      return message;
    }
  }

  return null;
}
