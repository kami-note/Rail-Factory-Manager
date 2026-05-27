import React, { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import { Plus, X } from 'lucide-react';
import { createMaterial } from '../api/materials';
import { toUiErrorMessage } from '../../../shared/lib/http';

export const UNIT_OPTIONS = ['UN', 'KG', 'G', 'L', 'ML', 'M', 'M2', 'M3', 'CX', 'PC', 'PAR'];

export function CreateMaterialModal({ open, tenantCode, category, onCreated, onClose }: {
  open: boolean;
  tenantCode: string;
  category: 'RawMaterial' | 'FinishedGood';
  onCreated: (code: string) => void;
  onClose: () => void;
}) {
  const isRawMaterial = category === 'RawMaterial';
  const title = isRawMaterial ? 'Nova Matéria-Prima' : 'Novo Produto Acabado';

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [unit, setUnit] = useState('UN');
  const [gtin, setGtin] = useState('');
  const [ncm, setNcm] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setCode(''); setName(''); setDescription('');
      setUnit('UN'); setGtin(''); setNcm('');
      setError(null); setSaving(false);
    }
  }, [open]);

  const handleSubmit = async () => {
    setSaving(true);
    setError(null);
    try {
      await createMaterial(tenantCode, {
        materialCode: code.trim().toUpperCase(),
        officialName: name.trim(),
        description: description.trim(),
        unitOfMeasure: unit,
        procurementType: isRawMaterial ? 'Buy' : 'Make',
        category,
        gtin: gtin.trim() || undefined,
        ncm: ncm.trim() || undefined,
      });
      onCreated(code.trim().toUpperCase());
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível cadastrar o material.'));
    } finally {
      setSaving(false);
    }
  };

  const canSubmit = code.trim().length >= 2 && name.trim().length >= 2 && description.trim().length >= 2;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        {title}
        <IconButton size="small" onClick={onClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}

          <Alert severity="info" icon={false} sx={{ fontSize: '0.8rem', py: 0.5 }}>
            {isRawMaterial
              ? 'Matéria-prima comprada de fornecedores. Usada como componente nas BOMs.'
              : 'Produto fabricado internamente. Aparece como produto nas BOMs e Ordens de Produção.'}
          </Alert>

          <Stack direction="row" spacing={2}>
            <TextField
              label="Código *"
              size="small"
              sx={{ width: 160 }}
              value={code}
              onChange={e => setCode(e.target.value.toUpperCase())}
              slotProps={{ input: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
              helperText="Ex: MAT-ACO-001"
            />
            <Select
              size="small"
              value={unit}
              onChange={e => setUnit(e.target.value)}
              sx={{ width: 100 }}
            >
              {UNIT_OPTIONS.map(u => <MenuItem key={u} value={u}>{u}</MenuItem>)}
            </Select>
          </Stack>

          <TextField
            label="Nome oficial *"
            size="small"
            fullWidth
            value={name}
            onChange={e => setName(e.target.value)}
            helperText="Ex: Aço carbono SAE 1020"
          />

          <TextField
            label="Descrição *"
            size="small"
            fullWidth
            multiline
            rows={2}
            value={description}
            onChange={e => setDescription(e.target.value)}
          />

          <Stack direction="row" spacing={2}>
            <TextField
              label="GTIN / EAN (opcional)"
              size="small"
              sx={{ flexGrow: 1 }}
              value={gtin}
              onChange={e => setGtin(e.target.value)}
            />
            <TextField
              label="NCM (opcional)"
              size="small"
              sx={{ width: 140 }}
              value={ncm}
              onChange={e => setNcm(e.target.value)}
            />
          </Stack>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !canSubmit}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <Plus size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Cadastrar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
