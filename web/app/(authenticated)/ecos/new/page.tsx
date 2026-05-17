"use client";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import SaveIcon from "@mui/icons-material/Save";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import FormControl from "@mui/material/FormControl";
import FormHelperText from "@mui/material/FormHelperText";
import Grid from "@mui/material/Grid";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
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

/**
 * Renders the authenticated new ECO creation route.
 *
 * @returns The dense ECO creation form page.
 */
export default function NewEcoPage() {
  const router = useRouter();
  const [form, setForm] = useState<CreateEcoFormState>(initialFormState);
  const [fieldErrors, setFieldErrors] = useState<CreateEcoFieldErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

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
      const createdId = readCreatedEcoId(createdEco);

      router.push(createdId ? `/ecos/${createdId}` : "/ecos");
    } catch (error) {
      setErrorMessage(getCreateEcoErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  /**
   * Updates a text form field and clears its current validation message.
   *
   * @param field - Form field to update.
   * @param value - New text field value.
   * @returns Nothing.
   */
  function handleTextFieldChange(
    field: "title" | "description",
    value: string,
  ): void {
    setForm((current) => ({ ...current, [field]: value }));
    clearFieldError(field);
  }

  /**
   * Updates the selected priority and clears its current validation message.
   *
   * @param event - Material UI select change event.
   * @returns Nothing.
   */
  function handlePriorityChange(event: SelectChangeEvent<CreateEcoPriority>): void {
    const nextPriority = event.target.value as CreateEcoPriority;

    setForm((current) => ({ ...current, priority: nextPriority }));
    clearFieldError("priority");
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

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title="New Engineering Change Order"
        description="Create a draft ECO for review by the engineering approval team."
        actionButton={
          <Button
            component={NextLink}
            href="/ecos"
            type="button"
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            sx={{
              width: { xs: "100%", sm: "auto" },
              textTransform: "none",
            }}
          >
            Back to ECOs
          </Button>
        }
      />

      <Paper
        component="section"
        elevation={1}
        sx={{
          p: { xs: 2, sm: 3 },
          maxWidth: 960,
        }}
      >
        <Box component="form" noValidate onSubmit={handleSubmit}>
          <Stack spacing={2.5}>
            {errorMessage ? (
              <Alert severity="error" variant="outlined">
                {errorMessage}
              </Alert>
            ) : null}

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 8 }}>
                <TextField
                  id="title"
                  name="title"
                  label="Title"
                  value={form.title}
                  onChange={(event) =>
                    handleTextFieldChange("title", event.target.value)
                  }
                  disabled={isSubmitting}
                  error={Boolean(fieldErrors.title)}
                  helperText={
                    fieldErrors.title ?? `${form.title.length}/${titleMaxLength}`
                  }
                  slotProps={{
                    htmlInput: {
                      maxLength: titleMaxLength,
                    },
                  }}
                  required
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormControl
                  fullWidth
                  required
                  size="small"
                  disabled={isSubmitting}
                  error={Boolean(fieldErrors.priority)}
                >
                  <InputLabel id="priority-label">Priority</InputLabel>
                  <Select
                    labelId="priority-label"
                    id="priority"
                    name="priority"
                    value={form.priority}
                    label="Priority"
                    onChange={handlePriorityChange}
                  >
                    {createEcoPriorityOptions.map((priority) => (
                      <MenuItem key={priority} value={priority}>
                        {priority}
                      </MenuItem>
                    ))}
                  </Select>
                  <FormHelperText>
                    {fieldErrors.priority ?? "Required for review triage."}
                  </FormHelperText>
                </FormControl>
              </Grid>
              <Grid size={12}>
                <TextField
                  id="description"
                  name="description"
                  label="Description"
                  value={form.description}
                  onChange={(event) =>
                    handleTextFieldChange("description", event.target.value)
                  }
                  disabled={isSubmitting}
                  error={Boolean(fieldErrors.description)}
                  helperText={
                    fieldErrors.description ??
                    `${form.description.length}/${descriptionMaxLength}`
                  }
                  multiline
                  minRows={4}
                  slotProps={{
                    htmlInput: {
                      maxLength: descriptionMaxLength,
                    },
                  }}
                  required
                  fullWidth
                  size="small"
                />
              </Grid>
            </Grid>

            <Stack
              direction={{ xs: "column-reverse", sm: "row" }}
              spacing={1.5}
              sx={{ justifyContent: "flex-end" }}
            >
              <Button
                component={NextLink}
                href="/ecos"
                type="button"
                variant="outlined"
                disabled={isSubmitting}
                sx={{ textTransform: "none" }}
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
                  minWidth: 132,
                  textTransform: "none",
                }}
              >
                {isSubmitting ? (
                  <CircularProgress color="inherit" size={20} thickness={5} />
                ) : (
                  "Create ECO"
                )}
              </Button>
            </Stack>

            <Typography variant="caption" color="text.secondary">
              New ECOs are created in Draft status and can be submitted for
              review from the detail page.
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Stack>
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
