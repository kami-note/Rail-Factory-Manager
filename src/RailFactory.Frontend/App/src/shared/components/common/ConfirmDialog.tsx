import React from 'react';
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from '@mui/material';

export type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** 'error' = vermelho (ação destrutiva), 'warning' = laranja, 'primary' = padrão */
  severity?: 'error' | 'warning' | 'primary';
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
};

/**
 * Caixa de diálogo genérica de confirmação.
 * Usada para ações irreversíveis ou potencialmente destrutivas.
 *
 * @example
 * <ConfirmDialog
 *   open={confirmOpen}
 *   title="Inativar veículo?"
 *   message="O veículo ABC-1234 não poderá receber novas alocações enquanto estiver inativo."
 *   severity="warning"
 *   confirmLabel="Inativar"
 *   loading={deactivating}
 *   onConfirm={handleConfirm}
 *   onCancel={() => setConfirmOpen(false)}
 * />
 */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  severity = 'primary',
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const colorMap = {
    error: 'error',
    warning: 'warning',
    primary: 'primary',
  } as const;

  return (
    <Dialog open={open} onClose={loading ? undefined : onCancel} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ fontWeight: 800 }}>{title}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary">
          {message}
        </Typography>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onCancel} disabled={loading}>
          {cancelLabel}
        </Button>
        <Button
          variant="contained"
          color={colorMap[severity]}
          onClick={onConfirm}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} color="inherit" /> : undefined}
          sx={{ fontWeight: 800 }}
        >
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
