"use client";

import React, { createContext, useContext, useMemo, useCallback, useState } from "react";
import en from "@/locales/en.json";
import ptBR from "@/locales/pt-BR.json";

type Locale = "en" | "pt-BR";
type Translations = typeof en;

interface I18nContextProps {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}

const COOKIE_NAME = "engi-locale";
const dictionaries: Record<Locale, Translations> = {
  en: en as Translations,
  "pt-BR": ptBR as Translations,
};

const I18nContext = createContext<I18nContextProps | undefined>(undefined);

/**
 * High-performance I18n Context Provider.
 * Orchestrates isomorphic translation resource delivery and state management.
 *
 * @param props - Component properties including initial locale.
 * @returns The hydrated context provider.
 */
export function I18nProvider({ 
  children, 
  initialLocale = "en" 
}: { 
  children: React.ReactNode; 
  initialLocale?: Locale;
}) {
  const [locale, setLocaleState] = useState<Locale>(() => {
    if (typeof window === "undefined") return initialLocale;
    
    const cookieValue = document.cookie
      .split("; ")
      .find((row) => row.startsWith(`${COOKIE_NAME}=`))
      ?.split("=")[1] as Locale;

    if (cookieValue && (cookieValue === "en" || cookieValue === "pt-BR")) {
      return cookieValue;
    }
    return initialLocale;
  });

  const setLocale = useCallback((newLocale: Locale) => {
    setLocaleState(newLocale);
    // Persist to cookie for middleware/proxy detection
    document.cookie = `${COOKIE_NAME}=${newLocale}; path=/; max-age=${60 * 60 * 24 * 365}; SameSite=Lax`;
  }, []);

  /**
   * Resolves a dot-notated key against the active dictionary.
   * Supports dynamic parameter injection (e.g., {count}).
   */
  const t = useCallback((key: string, params?: Record<string, string | number>): string => {
    const dictionary = dictionaries[locale];
    const keys = key.split(".");
    
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let value: any = dictionary;
    for (const k of keys) {
      value = value?.[k];
    }

    if (typeof value !== "string") {
      console.warn(`[I18n] Translation key not found or not a string: ${key}`);
      return key;
    }

    if (params) {
      return Object.entries(params).reduce(
        (acc, [paramKey, paramValue]) => acc.replace(`{${paramKey}}`, String(paramValue)),
        value
      );
    }

    return value;
  }, [locale]);

  const contextValue = useMemo(() => ({
    locale,
    setLocale,
    t,
  }), [locale, setLocale, t]);

  return (
    <I18nContext.Provider value={contextValue}>
      {children}
    </I18nContext.Provider>
  );
}

/**
 * Enterprise hook for consuming translation resources.
 * 
 * @returns The active I18n context state and translation function.
 * @throws Error if used outside of I18nProvider.
 */
export function useTranslation() {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error("useTranslation must be used within an I18nProvider");
  }
  return context;
}
