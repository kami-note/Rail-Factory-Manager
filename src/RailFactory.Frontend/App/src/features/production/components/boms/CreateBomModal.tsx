import React, { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { Plus, X } from 'lucide-react';
import { createBom } from '../../api/production';
import { MaterialCodeAutocomplete } from '../../../inventory';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import type { Bom } from '../../types';

export function CreateBomModal({
  open,
  tenantCode,
  onCreated,
  onClose,
}: {
  open: boolean;
  tenantCode: string;
  onCreated: (bom: Bom) => void;
  onClose: () => void;
}) {
  const [productInput, setProductInput] = useState('');
  const [selectedCode, setSelectedCode] = useState<string | null>(null);
  const [batchSize, setBatchSize] = useState('1.0');
  const justSelectedRef = useRef<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset state when modal opens/closes
  useEffect(() => {
    if (!open) {
      setProductInput('');
      setSelectedCode(null);
      setBatchSize('1.0');
      setError(null);
      setSaving(false);
      justSelectedRef.current = null;
    }
  }, [open]);

  const handleSubmit = async () => {
    if (!selectedCode) return;
    setSaving(true);
    setError(null);

    const parsedBatchSize = parseFloat(batchSize);
    const finalBatchSize = isNaN(parsedBatchSize) || parsedBatchSize <= 0 ? 1.0 : parsedBatchSize;

    try {
      const newBom = await createBom(tenantCode, { productCode: selectedCode, batchSize: finalBatchSize });
      onCreated(newBom);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar a BOM.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        Nova Estrutura de Produto (BOM)
        <IconButton size="small" onClick={onClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={3} sx={{ pt: 1 }}>
          {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}

          <Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
              Selecione o produto acabado para o qual deseja criar a estrutura. O produto deve estar
              cadastrado no catálogo de inventário como <strong>Produto Acabado</strong>.
            </Typography>
            <MaterialCodeAutocomplete
              tenantCode={tenantCode}
              value={productInput}
              onInputChange={code => {
                setProductInput(code);
                if (code !== justSelectedRef.current) {
                  setSelectedCode(null);
                }
                justSelectedRef.current = null;
              }}
              onMaterialSelect={m => {
                justSelectedRef.current = m.materialCode;
                setProductInput(m.materialCode);
                setSelectedCode(m.materialCode);
              }}
              label="Produto"
              placeholder="Digite para buscar..."
              fullWidth
              category="FinishedGood"
            />
            {selectedCode && (
              <Alert severity="success" icon={false} sx={{ mt: 1, py: 0.5, fontSize: '0.78rem' }}>
                Produto selecionado: <strong>{selectedCode}</strong>
              </Alert>
            )}
            {productInput.length >= 2 && !selectedCode && (
              <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                Selecione um produto da lista de sugestões.
              </Typography>
            )}

            <TextField
              label="Lote Padrão (Batch Size)"
              type="number"
              value={batchSize}
              onChange={e => setBatchSize(e.target.value)}
              placeholder="Ex: 1.0, 100"
              fullWidth
              sx={{ mt: 2 }}
              inputProps={{ min: "0.0001", step: "any" }}
              helperText="Define a quantidade base produzida por esta receita. O padrão é 1.0."
            />
          </Box>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saving}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !selectedCode}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <Plus size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Criar BOM
        </Button>
      </DialogActions>
    </Dialog>
  );
}
