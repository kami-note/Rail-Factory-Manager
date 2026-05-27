import React, { useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
} from '@mui/material';
import { addBomItem, getBom } from '../../api/production';
import { MaterialCodeAutocomplete } from '../../../inventory';
import { InlineError } from '../../../../shared/components/common/InlineError';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import type { Bom } from '../../types';

export function AddBomItemForm({
  tenantCode,
  bom,
  onAdded,
  onCancel,
}: {
  tenantCode: string;
  bom: Bom;
  onAdded: (updated: Bom) => void;
  onCancel: () => void;
}) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [unitOfMeasure, setUnitOfMeasure] = useState('UN');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    if (!materialCode.trim() || !quantity || !unitOfMeasure.trim()) return;
    setSaving(true);
    setError(null);
    try {
      await addBomItem(tenantCode, bom.id, {
        materialCode: materialCode.trim().toUpperCase(),
        quantity: Number(quantity),
        unitOfMeasure: unitOfMeasure.trim().toUpperCase(),
      });
      const freshBom = await getBom(tenantCode, bom.id);
      onAdded(freshBom);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível adicionar o componente.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box sx={{ mt: 1 }}>
      {error && <InlineError message={error} marginBottom={1} />}
      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
        <MaterialCodeAutocomplete
          tenantCode={tenantCode}
          value={materialCode}
          onInputChange={setMaterialCode}
          onMaterialSelect={m => {
            setMaterialCode(m.materialCode);
            setUnitOfMeasure(m.unitOfMeasure.toUpperCase());
          }}
          label="Código do material"
          sx={{ flexGrow: 1 }}
          category="RawMaterial"
        />
        <TextField
          label="Qtd"
          type="number"
          size="small"
          sx={{ width: 90 }}
          value={quantity}
          onChange={e => setQuantity(e.target.value)}
        />
        <TextField
          label="Unidade"
          size="small"
          sx={{ width: 90 }}
          value={unitOfMeasure}
          slotProps={{ input: { readOnly: true } }}
        />
        <Button
          variant="contained"
          size="small"
          onClick={() => void handleSubmit()}
          disabled={saving || !materialCode.trim()}
          sx={{ fontWeight: 800, alignSelf: 'center' }}
        >
          {saving ? <CircularProgress size={16} color="inherit" /> : 'Salvar'}
        </Button>
        <Button size="small" onClick={onCancel} disabled={saving} sx={{ alignSelf: 'center' }}>
          Cancelar
        </Button>
      </Stack>
    </Box>
  );
}
