import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { Plus, X } from 'lucide-react';
import { listBoms, createProductionOrder } from '../../api/production';
import { MaterialCodeAutocomplete } from '../../../inventory';
import { InlineError } from '../../../../shared/components/common/InlineError';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import type { ProductionOrder, WorkCenter, Bom } from '../../types';

export function CreateOrderModal({ open, tenantCode, workCenters, onCreated, onClose }: {
  open: boolean;
  tenantCode: string;
  workCenters: WorkCenter[];
  onCreated: (order: ProductionOrder) => void;
  onClose: () => void;
}) {
  const [productCode, setProductCode] = useState('');
  const [boms, setBoms] = useState<Bom[]>([]);
  const [bomsLoading, setBomsLoading] = useState(false);
  const [selectedBomId, setSelectedBomId] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [plannedQuantity, setPlannedQuantity] = useState('1');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activeWorkCenters = workCenters.filter(wc => wc.status.key === 'Active');

  const handleProductSelect = async (code: string) => {
    setProductCode(code);
    if (!code.trim()) { setBoms([]); setSelectedBomId(''); return; }
    setBomsLoading(true);
    setSelectedBomId('');
    setBoms([]);
    try {
      const found = await listBoms(tenantCode, code.trim().toUpperCase());
      setBoms(found);
      const active = found.find(b => b.status.key === 'Active');
      if (active) setSelectedBomId(active.id);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível buscar as BOMs.'));
    } finally {
      setBomsLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!selectedBomId || !workCenterId || !plannedQuantity) return;
    setSaving(true);
    setError(null);
    try {
      const order = await createProductionOrder(tenantCode, { bomId: selectedBomId, workCenterId, plannedQuantity: Number(plannedQuantity) });
      onCreated(order);
      onClose();
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar a ordem de produção.'));
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => {
    if (saving) return;
    setProductCode('');
    setBoms([]);
    setSelectedBomId('');
    setWorkCenterId('');
    setPlannedQuantity('1');
    setError(null);
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        Nova Ordem de Produção
        <IconButton size="small" onClick={handleClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <InlineError message={error} marginBottom={0} />}

          <MaterialCodeAutocomplete
            tenantCode={tenantCode}
            value={productCode}
            onInputChange={code => {
              setProductCode(code);
              if (!code) { setBoms([]); setSelectedBomId(''); }
            }}
            onMaterialSelect={m => void handleProductSelect(m.materialCode)}
            label="Produto"
            fullWidth
            category="FinishedGood"
          />

          {bomsLoading && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <CircularProgress size={16} />
              <Typography variant="caption" color="text.secondary">Buscando BOMs...</Typography>
            </Box>
          )}

          {!bomsLoading && boms.length > 0 && (
            <FormControl size="small" fullWidth>
              <InputLabel>Estrutura (BOM)</InputLabel>
              <Select label="Estrutura (BOM)" value={selectedBomId} onChange={e => setSelectedBomId(e.target.value)}>
                {boms.map(b => (
                  <MenuItem key={b.id} value={b.id}>
                    v{b.version} — {b.status.key === 'Active' ? 'Ativa' : 'Rascunho'} — {b.items.length} componente(s)
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {!bomsLoading && productCode && boms.length === 0 && (
            <Alert severity="warning" sx={{ py: 0.5 }}>Nenhuma BOM encontrada para este produto.</Alert>
          )}

          <Stack direction="row" spacing={2}>
            <FormControl size="small" sx={{ flexGrow: 1 }}>
              <InputLabel>Centro de Trabalho</InputLabel>
              <Select label="Centro de Trabalho" value={workCenterId} onChange={e => setWorkCenterId(e.target.value)}>
                {activeWorkCenters.map(wc => <MenuItem key={wc.id} value={wc.id}>{wc.name}</MenuItem>)}
              </Select>
            </FormControl>
            <TextField
              label="Qtd Planejada"
              type="number"
              size="small"
              sx={{ width: 140 }}
              value={plannedQuantity}
              onChange={e => setPlannedQuantity(e.target.value)}
            />
          </Stack>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !selectedBomId || !workCenterId || !plannedQuantity}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <Plus size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Criar Ordem
        </Button>
      </DialogActions>
    </Dialog>
  );
}
