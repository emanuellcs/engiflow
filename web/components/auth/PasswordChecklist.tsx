"use client";

import CheckCircle from "@mui/icons-material/CheckCircle";
import RadioButtonUnchecked from "@mui/icons-material/RadioButtonUnchecked";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";

/**
 * Describes the props used by the shared password checklist.
 */
interface PasswordChecklistProps {
  password: string;
}

/**
 * Defines the password rules shown while users type a new password.
 */
const passwordCriteria = [
  { label: "12+ characters", test: (value: string) => value.length >= 12 },
  { label: "1 Uppercase", test: (value: string) => /[A-Z]/.test(value) },
  { label: "1 Lowercase", test: (value: string) => /[a-z]/.test(value) },
  { label: "1 Number", test: (value: string) => /[0-9]/.test(value) },
  { label: "1 Symbol", test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
];

/**
 * Renders the live five-point password strength checklist.
 */
export default function PasswordChecklist({ password }: PasswordChecklistProps) {
  return (
    <Stack spacing={0.5} sx={{ pl: 0.5 }}>
      {passwordCriteria.map((criterion) => {
        const met = criterion.test(password);

        return (
          <Stack
            key={criterion.label}
            direction="row"
            spacing={1}
            sx={{ alignItems: "center" }}
          >
            {met ? (
              <CheckCircle sx={{ fontSize: 16, color: "success.main" }} />
            ) : (
              <RadioButtonUnchecked sx={{ fontSize: 16, color: "text.disabled" }} />
            )}
            <Typography
              variant="caption"
              color={met ? "success.main" : "text.secondary"}
            >
              {criterion.label}
            </Typography>
          </Stack>
        );
      })}
    </Stack>
  );
}
