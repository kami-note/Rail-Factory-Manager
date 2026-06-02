import React, { useEffect, useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, Button, IconButton, Stack, Grid, Typography, Divider,
} from '@mui/material';
import { X } from 'lucide-react';
import { addShipmentItem } from '../api/logistics';
import type { ShipmentItem } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = {
  open: boolean;
  tenantCode: string;
  orderId: string;
  orderNumber: string;
  onAdded: (item: ShipmentItem) => void;
  onClose: () => void;
};

interface FormState {
  materialCode: string;
  quantity: string;
  unitOfMeasure: string;
  weightKg: string;
  volumeCbm: string;
  ncmCode: string;
  cfopCode: string;
  unitValue: string;
  taxBaseIcms: string;
  icmsRate: string;
}

const EMPTY: FormState = {
  materialCode: '', quantity: '', unitOfMeasure: 'UN',
  weightKg: '', volumeCbm: '',
  ncmCode: '', cfopCode: '5102', unitValue: '', taxBaseIcms: '', icmsRate: '12',
};

export function AddShipmentItemModal({ open, tenantCode, orderId, orderNumber, onAdded, onClose }: Props) {
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
    const qty = parseFloat(form.quantity);
    if (isNaN(qty) || qty <= 0) {
      setError('Quantidade inválida.');
      setSaving(false);
      return;
    }

    try {
      const item = await addShipmentItem(tenantCode, orderId, {
        materialCode: form.materialCode.trim().toUpperCase(),
        quantity: qty,
        unitOfMeasure: form.unitOfMeasure.trim(),
        weightKg: parseFloat(form.weightKg || '0'),
        volumeCbm: parseFloat(form.volumeCbm || '0'),
        ncmCode: form.ncmCode.trim() || undefined,
        cfopCode: form.cfopCode.trim() || undefined,
        unitValue: parseFloat(form.unitValue || '0'),
        taxBaseIcms: parseFloat(form.taxBaseIcms || '0'),
        icmsRate: parseFloat(form.icmsRate || '12'),
      });
      onAdded(item);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao adicionar item.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Adicionar Item — {orderNumber}
          <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>

        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error" variant="body2">{error}</Typography>}

            <Grid container spacing={1}>
              <Grid item xs={6}>
                <TextField label="Código do Material *" value={form.materialCode} onChange={set('materialCode')} fullWidth size="small" required />
              </Grid>
              <Grid item xs={3}>
                <TextField label="Qtd *" value={form.quantity} onChange={set('quantity')} fullWidth size="small" required type="number" inputProps={{ min: 0.001, step: 0.001 }} />
              </Grid>
              <Grid item xs={3}>
                <TextField label="UN *" value={form.unitOfMeasure} onChange={set('unitOfMeasure')} fullWidth size="small" required />
              </Grid>
              <Grid item xs={6}>
                <TextField label="Peso (kg)" value={form.weightKg} onChange={set('weightKg')} fullWidth size="small" type="number" inputProps={{ min: 0, step: 0.001 }} />
              </Grid>
              <Grid item xs={6}>
                <TextField label="Volume (m³)" value={form.volumeCbm} onChange={set('volumeCbm')} fullWidth size="small" type="number" inputProps={{ min: 0, step: 0.0001 }} />
              </Grid>
            </Grid>

            <Divider>
              <Typography variant="caption" color="text.secondary">Dados Fiscais NF-e</Typography>
            </Divider>

            <Grid container spacing={1}>
              <Grid item xs={4}>
                <TextField label="NCM" value={form.ncmCode} onChange={set('ncmCode')} fullWidth size="small" placeholder="84713012" inputProps={{ maxLength: 10 }} />
              </Grid>
              <Grid item xs={4}>
                <TextField label="CFOP" value={form.cfopCode} onChange={set('cfopCode')} fullWidth size="small" placeholder="5102" inputProps={{ maxLength: 5 }} />
              </Grid>
              <Grid item xs={4}>
                <TextField label="Alíquota ICMS (%)" value={form.icmsRate} onChange={set('icmsRate')} fullWidth size="small" type="number" />
              </Grid>
              <Grid item xs={6}>
                <TextField label="Valor Unitário (R$)" value={form.unitValue} onChange={set('unitValue')} fullWidth size="small" type="number" inputProps={{ step: 0.01 }} />
              </Grid>
              <Grid item xs={6}>
                <TextField label="Base Cálculo ICMS (R$)" value={form.taxBaseIcms} onChange={set('taxBaseIcms')} fullWidth size="small" type="number" inputProps={{ step: 0.01 }} />
              </Grid>
            </Grid>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={saving || !form.materialCode || !form.quantity}>
            {saving ? 'Adicionando...' : 'Adicionar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
