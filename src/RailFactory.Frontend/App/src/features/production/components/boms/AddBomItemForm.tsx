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
  const [scrapFactorPercent, setScrapFactorPercent] = useState('0');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    if (!materialCode.trim() || !quantity || !unitOfMeasure.trim()) return;
    
    const loss = Number(scrapFactorPercent);
    if (isNaN(loss) || loss < 0 || loss >= 100) {
      setError('A perda técnica deve ser entre 0% e 99.99%.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      await addBomItem(tenantCode, bom.id, {
        materialCode: materialCode.trim().toUpperCase(),
        quantity: Number(quantity),
        unitOfMeasure: unitOfMeasure.trim().toUpperCase(),
        scrapFactor: loss / 100,
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
          sx={{ width: 80 }}
          value={quantity}
          onChange={e => setQuantity(e.target.value)}
        />
        <TextField
          label="Unidade"
          size="small"
          sx={{ width: 80 }}
          value={unitOfMeasure}
          slotProps={{ input: { readOnly: true } }}
        />
        <TextField
          label="Perda (%)"
          type="number"
          size="small"
          sx={{ width: 90 }}
          value={scrapFactorPercent}
          onChange={e => setScrapFactorPercent(e.target.value)}
          slotProps={{ htmlInput: { min: 0, max: 99.99, step: 0.1 } }}
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
