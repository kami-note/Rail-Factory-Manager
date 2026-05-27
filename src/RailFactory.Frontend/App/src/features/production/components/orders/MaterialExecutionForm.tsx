import React, { useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Stack,
  TextField,
} from '@mui/material';
import { recordConsumption, recordScrap } from '../../api/production';
import { MaterialCodeAutocomplete } from '../../../inventory';
import { Authorized } from '../../../auth';
import { toUiErrorMessage } from '../../../../shared/lib/http';

type Props = {
  tenantCode: string;
  orderId: string;
  mode: 'consumption' | 'scrap';
  onRecorded: () => void;
};

export function MaterialExecutionForm({ tenantCode, orderId, mode, onRecorded }: Props) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('');
  const [unit, setUnit] = useState('UN');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const isScrap = mode === 'scrap';
  const buttonLabel = isScrap ? 'Registrar Scrap' : 'Registrar Consumo';
  const successMessage = isScrap ? 'Scrap registrado.' : 'Consumo registrado.';
  const errorFallback = isScrap
    ? 'Não foi possível registrar o scrap.'
    : 'Não foi possível registrar o consumo.';
  const canSubmit = !!materialCode.trim() && !!quantity && (!isScrap || !!reason.trim());

  const handleSubmit = async () => {
    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      if (isScrap) {
        await recordScrap(tenantCode, orderId, {
          materialCode: materialCode.trim().toUpperCase(),
          scrapQuantity: Number(quantity),
          unitOfMeasure: unit.trim().toUpperCase(),
          reason: reason.trim(),
        });
        setReason('');
      } else {
        await recordConsumption(tenantCode, orderId, {
          materialCode: materialCode.trim().toUpperCase(),
          consumedQuantity: Number(quantity),
          unitOfMeasure: unit.trim().toUpperCase(),
        });
      }
      setSuccess(true);
      setMaterialCode('');
      setQuantity('');
      onRecorded();
    } catch (err) {
      setError(toUiErrorMessage(err, errorFallback));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Stack spacing={2}>
      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)}>{successMessage}</Alert>}
      <MaterialCodeAutocomplete
        tenantCode={tenantCode}
        value={materialCode}
        onInputChange={setMaterialCode}
        onMaterialSelect={m => { setMaterialCode(m.materialCode); setUnit(m.unitOfMeasure.toUpperCase()); }}
        label="Código do material"
        fullWidth
        category="RawMaterial"
      />
      <Stack direction="row" spacing={1}>
        <TextField label="Quantidade" type="number" size="small" sx={{ flexGrow: 1 }} value={quantity} onChange={e => setQuantity(e.target.value)} />
        <TextField label="Unidade" size="small" sx={{ width: 80 }} value={unit} slotProps={{ input: { readOnly: true } }} />
      </Stack>
      {isScrap && (
        <TextField label="Motivo" size="small" fullWidth multiline rows={2} value={reason} onChange={e => setReason(e.target.value)} />
      )}
      <Authorized permission="production.write">
        <Button
          variant="contained"
          color={isScrap ? 'warning' : 'primary'}
          fullWidth
          onClick={() => void handleSubmit()}
          disabled={saving || !canSubmit}
          sx={{ fontWeight: 800 }}
        >
          {saving ? <CircularProgress size={18} color="inherit" /> : buttonLabel}
        </Button>
      </Authorized>
    </Stack>
  );
}
