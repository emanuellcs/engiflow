"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import Button from "@mui/material/Button";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Typography from "@mui/material/Typography";
import LanguageIcon from "@mui/icons-material/Language";
import CheckIcon from "@mui/icons-material/Check";
import { useTranslation } from "@/context/I18nContext";

/**
 * Enterprise Language Switcher Component.
 * Provides a high-fidelity interaction for real-time locale toggling.
 *
 * @returns An interactive MUI-based language selector.
 */
export default function LanguageSwitcher() {
  const { locale, setLocale } = useTranslation();
  const router = useRouter();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const handleOpen = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleSelect = (newLocale: "en" | "pt-BR") => {
    if (newLocale !== locale) {
      setLocale(newLocale);
      // Trigger a seamless client-side state rehydration
      router.refresh();
    }
    handleClose();
  };

  return (
    <>
      <Button
        size="small"
        color="inherit"
        onClick={handleOpen}
        startIcon={<LanguageIcon fontSize="small" />}
        sx={{
          textTransform: "none",
          fontWeight: 600,
          fontSize: "0.8125rem",
          minWidth: 0,
          px: { xs: 0, sm: 1 },
          width: { xs: 40, sm: "auto" },
          height: { xs: 40, sm: "auto" },
          borderRadius: { xs: "50%", sm: 1 },
          color: "text.secondary",
          "&:hover": { color: "primary.main" },
          "& .MuiButton-startIcon": {
            mx: { xs: 0, sm: "inherit" },
          },
        }}
      >
        <Typography variant="inherit" sx={{ display: { xs: "none", sm: "inline" } }}>
          {locale === "en" ? "English" : "Português"}
        </Typography>
      </Button>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleClose}
        transformOrigin={{ horizontal: "right", vertical: "top" }}
        anchorOrigin={{ horizontal: "right", vertical: "bottom" }}
        slotProps={{
          paper: {
            sx: { mt: 1, minWidth: 160, borderRadius: 2, boxShadow: 4 },
          },
        }}
      >
        <MenuItem onClick={() => handleSelect("en")} selected={locale === "en"}>
          <ListItemIcon>
            {locale === "en" && <CheckIcon fontSize="small" color="primary" />}
          </ListItemIcon>
          <ListItemText primary="English (US)" />
        </MenuItem>
        <MenuItem onClick={() => handleSelect("pt-BR")} selected={locale === "pt-BR"}>
          <ListItemIcon>
            {locale === "pt-BR" && <CheckIcon fontSize="small" color="primary" />}
          </ListItemIcon>
          <ListItemText primary="Português (BR)" />
        </MenuItem>
      </Menu>
    </>
  );
}
