import { Alert } from '@mui/material';

type InlineErrorProps = {
  message: string;
  onClose?: () => void;
  marginBottom?: number;
};

export function InlineError({ message, onClose, marginBottom = 0 }: InlineErrorProps) {
  return (
    <Alert severity="error" onClose={onClose} sx={{ mb: marginBottom }}>
      {message}
    </Alert>
  );
}
