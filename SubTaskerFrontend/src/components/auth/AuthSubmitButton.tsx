type AuthSubmitButtonProps = {
  isSubmitting: boolean;
  label: string;
  submittingLabel: string;
};

export function AuthSubmitButton({ isSubmitting, label, submittingLabel }: AuthSubmitButtonProps) {
  return (
    <button
      className="mt-2 flex cursor-pointer items-center justify-between rounded bg-[#166b67] px-5 py-4 font-bold text-white transition hover:-translate-y-px hover:bg-[#0f504d] disabled:cursor-not-allowed disabled:opacity-60"
      type="submit"
      disabled={isSubmitting}
    >
      <span>{isSubmitting ? submittingLabel : label}</span>
      <span aria-hidden="true" className="text-xl font-normal">&rarr;</span>
    </button>
  );
}