"use client";

import CheckCircle from "@mui/icons-material/CheckCircle";
import RadioButtonUnchecked from "@mui/icons-material/RadioButtonUnchecked";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTranslation } from "@/context/I18nContext";

/**
 * Describes the props used by the shared password checklist.
 */
interface PasswordChecklistProps {
  password: string;
}

/**
 * Renders the live five-point password strength checklist.
 */
export default function PasswordChecklist({ password }: PasswordChecklistProps) {
  const { t } = useTranslation();

  /**
   * Defines the password rules shown while users type a new password.
   */
  const passwordCriteria = [
    { label: t("auth.register.passwordCriteria.length"), test: (value: string) => value.length >= 12 },
    { label: t("auth.register.passwordCriteria.uppercase"), test: (value: string) => /[A-Z]/.test(value) },
    { label: t("auth.register.passwordCriteria.lowercase"), test: (value: string) => /[a-z]/.test(value) },
    { label: t("auth.register.passwordCriteria.number"), test: (value: string) => /[0-9]/.test(value) },
    { label: t("auth.register.passwordCriteria.symbol"), test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
  ];

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
