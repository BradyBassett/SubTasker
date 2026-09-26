import type { Metadata } from "next";
import "./tailwind.css";

export const metadata: Metadata = {
  title: "SubTasker",
  description: "Your personal task management solution.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
