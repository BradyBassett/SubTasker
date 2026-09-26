import type { InputHTMLAttributes, ReactNode } from "react";

type AuthFieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  trailingContent?: ReactNode;
};

export function AuthField({ label, trailingContent, id, ...inputProps }: AuthFieldProps) {
  return (
    <div className="grid gap-2">
      <div className="flex items-center justify-between gap-4">
        <label className="text-xs font-bold" htmlFor={id}>{label}</label>
        {trailingContent}
      </div>
      <input
        {...inputProps}
        className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
        id={id}
      />
    </div>
  );
}