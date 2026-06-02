import React, { useEffect, useState } from 'react';
import {
  Accordion, AccordionDetails, AccordionSummary,
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, Button, IconButton, Stack, Grid, Typography,
} from '@mui/material';
import { ChevronDown, X } from 'lucide-react';
import { createShipmentOrder } from '../api/logistics';
import type { ShipmentOrder } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { open: boolean; tenantCode: string; onCreated: (o: ShipmentOrder) => void; onClose: () => void };

interface FormState {
  notes: string;
  recipientCnpj: string;
  recipientName: string;
  recipientEmail: string;
  recipientStreet: string;
  recipientNumber: string;
  recipientDistrict: string;
  recipientCity: string;
  recipientState: string;
  recipientZipCode: string;
  natureOfOperation: string;
}

const EMPTY: FormState = {
  notes: '',
  recipientCnpj: '', recipientName: '', recipientEmail: '',
  recipientStreet: '', recipientNumber: '', recipientDistrict: '',
  recipientCity: '', recipientState: '', recipientZipCode: '',
  natureOfOperation: 'Venda de mercadoria',
};

export function CreateShipmentOrderModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [form, setForm] = useState<FormState>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setForm(EMPTY); setSaving(false); setError(null); }
  }, [open]);

  const set = (field: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [field]: e.target.value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    setSaving(true); setError(null);
    try {
      const order = await createShipmentOrder(tenantCode, {
        notes: form.notes.trim() || undefined,
        recipientCnpj: form.recipientCnpj.trim() || undefined,
        recipientName: form.recipientName.trim() || undefined,
        recipientEmail: form.recipientEmail.trim() || undefined,
        recipientStreet: form.recipientStreet.trim() || undefined,
        recipientNumber: form.recipientNumber.trim() || undefined,
        recipientDistrict: form.recipientDistrict.trim() || undefined,
        recipientCity: form.recipientCity.trim() || undefined,
        recipientState: form.recipientState.trim() || undefined,
        recipientZipCode: form.recipientZipCode.trim() || undefined,
        natureOfOperation: form.natureOfOperation.trim() || undefined,
      });
      onCreated(order);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar ordem de expedição.'));
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Nova Ordem de Expedição
          <IconButton onClick={handleClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>

        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error" variant="body2">{error}</Typography>}

            <TextField
              label="Observações (opcional)"
              value={form.notes}
              onChange={set('notes')}
              fullWidth multiline rows={2} size="small"
            />

            <Accordion disableGutters elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <AccordionSummary expandIcon={<ChevronDown size={16} />}>
                <Typography variant="body2" fontWeight={700}>
                  Destinatário / Dados Fiscais NF-e
                </Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Stack spacing={2}>
                  <Typography variant="caption" color="text.secondary">
                    Preencha para habilitar emissão automática de NF-e ao despachar.
                  </Typography>
                  <Grid container spacing={1}>
                    <Grid item xs={6}>
                      <TextField label="CNPJ / CPF" value={form.recipientCnpj} onChange={set('recipientCnpj')} fullWidth size="small" placeholder="00.000.000/0000-00" />
                    </Grid>
                    <Grid item xs={6}>
                      <TextField label="Razão Social / Nome" value={form.recipientName} onChange={set('recipientName')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField label="E-mail" value={form.recipientEmail} onChange={set('recipientEmail')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={8}>
                      <TextField label="Logradouro" value={form.recipientStreet} onChange={set('recipientStreet')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={4}>
                      <TextField label="Número" value={form.recipientNumber} onChange={set('recipientNumber')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={6}>
                      <TextField label="Bairro" value={form.recipientDistrict} onChange={set('recipientDistrict')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={6}>
                      <TextField label="CEP" value={form.recipientZipCode} onChange={set('recipientZipCode')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={8}>
                      <TextField label="Município" value={form.recipientCity} onChange={set('recipientCity')} fullWidth size="small" />
                    </Grid>
                    <Grid item xs={4}>
                      <TextField label="UF" value={form.recipientState} onChange={set('recipientState')} fullWidth size="small" inputProps={{ maxLength: 2 }} placeholder="SP" />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField label="Natureza da Operação" value={form.natureOfOperation} onChange={set('natureOfOperation')} fullWidth size="small" />
                    </Grid>
                  </Grid>
                </Stack>
              </AccordionDetails>
            </Accordion>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Criando...' : 'Criar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
