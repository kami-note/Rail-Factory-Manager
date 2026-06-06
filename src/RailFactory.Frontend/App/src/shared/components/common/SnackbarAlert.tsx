import React from 'react';
import { Alert, Snackbar } from '@mui/material';

type Props = {
  message: string | null;
  severity: 'success' | 'error' | 'info' | 'warning';
  onClose: () => void;
  duration?: number;
};

export function SnackbarAlert({ message, severity, onClose, duration = 3500 }: Props) {
  return (
    <Snackbar
      open={!!message}
      autoHideDuration={duration}
      onClose={onClose}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
    >
      <Alert severity={severity} onClose={onClose} sx={{ minWidth: 320 }}>
        {message}
      </Alert>
    </Snackbar>
  );
}
