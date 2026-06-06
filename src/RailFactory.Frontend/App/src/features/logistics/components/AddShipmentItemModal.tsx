import React, { useEffect, useState } from 'react';
import {
  Accordion, AccordionDetails, AccordionSummary, Alert,
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, Button, IconButton, Stack, Grid, Typography, CircularProgress,
} from '@mui/material';
import { ChevronDown, X } from 'lucide-react';
import { addShipmentItem } from '../api/logistics';
import type { ShipmentItem } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialCodeAutocomplete } from '../../inventory/components/MaterialCodeAutocomplete';
import type { MaterialSearchResult } from '../../inventory/types';

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
  icmsOrigin: string;
  icmsCst: string;
  pisCst: string;
  cofinsCst: string;
  ipiRate: string;
}

const EMPTY: FormState = {
  materialCode: '', quantity: '', unitOfMeasure: 'UN',
  weightKg: '', volumeCbm: '',
  ncmCode: '', cfopCode: '5102', unitValue: '', taxBaseIcms: '', icmsRate: '12',
  icmsOrigin: '0', icmsCst: '40', pisCst: '07', cofinsCst: '07', ipiRate: '0',
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

  const handleMaterialSelect = (m: MaterialSearchResult) => {
    setForm(prev => ({
      ...prev,
      materialCode: m.materialCode,
      unitOfMeasure: m.unitOfMeasure ?? prev.unitOfMeasure,
      ncmCode: m.ncm ?? prev.ncmCode,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    const qty = parseFloat(form.quantity);
    if (isNaN(qty) || qty <= 0) { setError('Quantidade inválida.'); return; }
    setSaving(true); setError(null);
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
        icmsOrigin: parseInt(form.icmsOrigin || '0', 10),
        icmsCst: form.icmsCst.trim(),
        pisCst: form.pisCst.trim(),
        cofinsCst: form.cofinsCst.trim(),
        ipiRate: parseFloat(form.ipiRate || '0'),
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
            {error && <Alert severity="error">{error}</Alert>}

            {/* Material — autocomplete com pré-preenchimento */}
            <MaterialCodeAutocomplete
              tenantCode={tenantCode}
              value={form.materialCode}
              onInputChange={v => setForm(prev => ({ ...prev, materialCode: v }))}
              onMaterialSelect={handleMaterialSelect}
              label="Material *"
              fullWidth
              size="small"
            />

            <Grid container spacing={1}>
              <Grid xs={5}>
                <TextField
                  label="Quantidade *"
                  value={form.quantity}
                  onChange={set('quantity')}
                  fullWidth size="small" required type="number"
                  slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                />
              </Grid>
              <Grid xs={3}>
                <TextField
                  label="Unidade"
                  value={form.unitOfMeasure}
                  onChange={set('unitOfMeasure')}
                  fullWidth size="small"
                  slotProps={{ htmlInput: { style: { textTransform: 'uppercase' } } }}
                />
              </Grid>
              <Grid xs={4}>
                <TextField
                  label="Valor Unit. (R$)"
                  value={form.unitValue}
                  onChange={set('unitValue')}
                  fullWidth size="small" type="number"
                  slotProps={{ htmlInput: { step: 0.01, min: 0 } }}
                />
              </Grid>
              <Grid xs={6}>
                <TextField
                  label="Peso (kg)"
                  value={form.weightKg}
                  onChange={set('weightKg')}
                  fullWidth size="small" type="number"
                  slotProps={{ htmlInput: { min: 0, step: 0.001 } }}
                />
              </Grid>
              <Grid xs={6}>
                <TextField
                  label="Volume (m³)"
                  value={form.volumeCbm}
                  onChange={set('volumeCbm')}
                  fullWidth size="small" type="number"
                  slotProps={{ htmlInput: { min: 0, step: 0.0001 } }}
                />
              </Grid>
            </Grid>

            {/* Dados fiscais — colapsado por padrão */}
            <Accordion disableGutters elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <AccordionSummary expandIcon={<ChevronDown size={14} />} sx={{ minHeight: 36, '& .MuiAccordionSummary-content': { my: 0.5 } }}>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                  Dados Fiscais NF-e (NCM, CFOP, CST…)
                </Typography>
              </AccordionSummary>
              <AccordionDetails sx={{ pt: 1 }}>
                <Grid container spacing={1}>
                  <Grid xs={4}><TextField label="NCM" value={form.ncmCode} onChange={set('ncmCode')} fullWidth size="small" placeholder="84713012" slotProps={{ htmlInput: { maxLength: 10 } }} /></Grid>
                  <Grid xs={4}><TextField label="CFOP" value={form.cfopCode} onChange={set('cfopCode')} fullWidth size="small" placeholder="5102" slotProps={{ htmlInput: { maxLength: 5 } }} /></Grid>
                  <Grid xs={4}><TextField label="Origem ICMS" value={form.icmsOrigin} onChange={set('icmsOrigin')} fullWidth size="small" type="number" /></Grid>
                  <Grid xs={3}><TextField label="CST ICMS" value={form.icmsCst} onChange={set('icmsCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                  <Grid xs={3}><TextField label="CST PIS" value={form.pisCst} onChange={set('pisCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                  <Grid xs={3}><TextField label="CST COFINS" value={form.cofinsCst} onChange={set('cofinsCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                  <Grid xs={3}><TextField label="Alíq. IPI (%)" value={form.ipiRate} onChange={set('ipiRate')} fullWidth size="small" type="number" /></Grid>
                  <Grid xs={4}><TextField label="Alíq. ICMS (%)" value={form.icmsRate} onChange={set('icmsRate')} fullWidth size="small" type="number" /></Grid>
                  <Grid xs={4}><TextField label="Base ICMS (R$)" value={form.taxBaseIcms} onChange={set('taxBaseIcms')} fullWidth size="small" type="number" /></Grid>
                </Grid>
              </AccordionDetails>
            </Accordion>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button
            type="submit"
            variant="contained"
            disabled={saving || !form.materialCode || !form.quantity}
            startIcon={saving ? <CircularProgress size={14} color="inherit" /> : undefined}
          >
            {saving ? 'Adicionando...' : 'Adicionar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
