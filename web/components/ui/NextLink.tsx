"use client";

import Link, { type LinkProps } from "next/link";
import {
  forwardRef,
  type AnchorHTMLAttributes,
  type ForwardedRef,
  type ReactElement,
} from "react";

export type NextLinkProps = LinkProps &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, keyof LinkProps>;

/**
 * Adapts Next.js App Router links for Material UI components that expect a
 * ref-forwarding anchor element through their component prop.
 *
 * @param props - Next.js link props plus standard anchor attributes.
 * @param ref - Forwarded anchor ref supplied by Material UI.
 * @returns A Next.js Link element that can be used as a Material UI link target.
 */
function NextLink(
  props: NextLinkProps,
  ref: ForwardedRef<HTMLAnchorElement>,
): ReactElement {
  return <Link ref={ref} {...props} />;
}

export default forwardRef<HTMLAnchorElement, NextLinkProps>(NextLink);
