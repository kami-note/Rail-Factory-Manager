import React, { useEffect, useState } from 'react';
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
} from '@mui/material';
import { Plus, X } from 'lucide-react';
import { InlineError } from '../../../shared/components/common/InlineError';
import { createWorkCenter } from '../api/production';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { WorkCenter } from '../types';

type Props = {
  open: boolean;
  tenantCode: string;
  onCreated: (wc: WorkCenter) => void;
  onClose: () => void;
};

export function CreateWorkCenterModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setCode(''); setName(''); setError(null); setSaving(false); }
  }, [open]);

  const isValid = code.trim().length > 0 && name.trim().length > 0;

  const handleSubmit = async () => {
    if (!isValid) return;
    setSaving(true);
    setError(null);
    try {
      const wc = await createWorkCenter(tenantCode, { code: code.trim().toUpperCase(), name: name.trim() });
      onCreated(wc);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar o centro de trabalho.'));
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        Novo Centro de Trabalho
        <IconButton size="small" onClick={handleClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <InlineError message={error} marginBottom={0} />}
          <TextField
            label="Código"
            size="small"
            fullWidth
            value={code}
            onChange={e => setCode(e.target.value.toUpperCase())}
            placeholder="SOLDA-01"
            slotProps={{ htmlInput: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
            autoFocus
          />
          <TextField
            label="Nome"
            size="small"
            fullWidth
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="Linha de Soldagem 01"
            onKeyDown={e => { if (e.key === 'Enter' && isValid) void handleSubmit(); }}
          />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !isValid}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <Plus size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Criar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
