import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { recordConsumption, recordScrap } from '../../api/production';
import { MaterialCodeAutocomplete } from '../../../inventory';
import { Authorized } from '../../../auth';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import type { BomItem } from '../../types';

type Props = {
  tenantCode: string;
  orderId: string;
  mode: 'consumption' | 'scrap';
  bomItems?: BomItem[];
  plannedQuantity?: number;
  onRecorded: () => void;
};

export function MaterialExecutionForm({ tenantCode, orderId, mode, bomItems, plannedQuantity, onRecorded }: Props) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('');
  const [unit, setUnit] = useState('UN');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const isScrap = mode === 'scrap';

  const fillFromBom = (item: BomItem) => {
    setMaterialCode(item.materialCode);
    setUnit(item.unitOfMeasure);
    if (plannedQuantity) {
      setQuantity(String(item.quantity * plannedQuantity));
    }
  };

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
      setError(toUiErrorMessage(err, isScrap ? 'Não foi possível registrar o scrap.' : 'Não foi possível registrar o consumo.'));
    } finally {
      setSaving(false);
    }
  };

  const canSubmit = !!materialCode.trim() && !!quantity && (!isScrap || !!reason.trim());

  return (
    <Stack spacing={1.5}>
      {error && <Alert severity="error" onClose={() => setError(null)} sx={{ py: 0.5 }}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)} sx={{ py: 0.5 }}>Registrado.</Alert>}

      {/* BOM quick-fill chips */}
      {bomItems && bomItems.length > 0 && (
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
            Clique para preencher:
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {bomItems.map(item => (
              <Chip
                key={item.id}
                label={item.materialCode}
                size="small"
                variant={materialCode === item.materialCode ? 'filled' : 'outlined'}
                color={materialCode === item.materialCode ? 'primary' : 'default'}
                onClick={() => fillFromBom(item)}
                sx={{ fontFamily: 'monospace', fontWeight: 600, cursor: 'pointer', fontSize: '0.72rem' }}
              />
            ))}
          </Box>
        </Box>
      )}

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
        <TextField
          label="Quantidade"
          type="number"
          size="small"
          sx={{ flexGrow: 1 }}
          value={quantity}
          onChange={e => setQuantity(e.target.value)}
        />
        <TextField
          label="UM"
          size="small"
          sx={{ width: 72 }}
          value={unit}
          slotProps={{ input: { readOnly: true } }}
        />
      </Stack>

      {isScrap && (
        <TextField
          label="Motivo do scrap"
          size="small"
          fullWidth
          multiline
          rows={2}
          value={reason}
          onChange={e => setReason(e.target.value)}
        />
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
          {saving
            ? <CircularProgress size={18} color="inherit" />
            : isScrap ? 'Registrar Scrap' : 'Registrar Consumo'}
        </Button>
      </Authorized>
    </Stack>
  );
}
