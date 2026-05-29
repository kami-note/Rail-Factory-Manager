import React, { useEffect, useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, Button, IconButton, Stack,
} from '@mui/material';
import { X } from 'lucide-react';
import { createCarrier } from '../api/logistics';
import type { Carrier } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { open: boolean; tenantCode: string; onCreated: (c: Carrier) => void; onClose: () => void };

export function CreateCarrierModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [name, setName] = useState('');
  const [documentNumber, setDocumentNumber] = useState('');
  const [contactEmail, setContactEmail] = useState('');
  const [ratePerKg, setRatePerKg] = useState('');
  const [ratePerCbm, setRatePerCbm] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setName(''); setDocumentNumber(''); setContactEmail('');
      setRatePerKg(''); setRatePerCbm(''); setSaving(false); setError(null);
    }
  }, [open]);

  const isValid = name.trim() && documentNumber.trim() && ratePerKg && ratePerCbm;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid || saving) return;
    setSaving(true); setError(null);
    try {
      const carrier = await createCarrier(tenantCode, {
        name: name.trim(), documentNumber: documentNumber.trim(),
        contactEmail: contactEmail.trim() || undefined,
        ratePerKg: parseFloat(ratePerKg), ratePerCbm: parseFloat(ratePerCbm),
      });
      onCreated(carrier);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao cadastrar transportadora.'));
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Nova Transportadora
          <IconButton onClick={handleClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <div style={{ color: 'red', fontSize: 13 }}>{error}</div>}
            <TextField label="Nome" value={name} onChange={e => setName(e.target.value)} required fullWidth autoFocus size="small" />
            <TextField label="CNPJ / Documento" value={documentNumber} onChange={e => setDocumentNumber(e.target.value)} required fullWidth size="small" />
            <TextField label="E-mail de contato" value={contactEmail} onChange={e => setContactEmail(e.target.value)} fullWidth size="small" type="email" />
            <TextField label="Taxa por kg (R$)" value={ratePerKg} onChange={e => setRatePerKg(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.0001' } }} />
            <TextField label="Taxa por m³ (R$)" value={ratePerCbm} onChange={e => setRatePerCbm(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.0001' } }} />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={!isValid || saving}>
            {saving ? 'Cadastrando...' : 'Cadastrar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
