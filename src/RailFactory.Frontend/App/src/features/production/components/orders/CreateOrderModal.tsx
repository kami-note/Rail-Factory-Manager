import React, { useEffect, useState } from 'react';
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
  const [boms, setBoms] = useState<Bom[]>([]);
  const [bomsLoading, setBomsLoading] = useState(false);
  const [selectedBomId, setSelectedBomId] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [plannedQuantity, setPlannedQuantity] = useState('1');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activeWorkCenters = workCenters.filter(wc => wc.status.key === 'Active');

  // Load active BOMs when dialog opens
  useEffect(() => {
    if (!open) return;
    setBomsLoading(true);
    listBoms(tenantCode)
      .then(all => setBoms(all.filter(b => b.status.key === 'Active')))
      .catch(err => setError(toUiErrorMessage(err, 'Não foi possível carregar as BOMs.')))
      .finally(() => setBomsLoading(false));
  }, [open, tenantCode]);

  const selectedBom = boms.find(b => b.id === selectedBomId);

  const handleSubmit = async () => {
    if (!selectedBomId || !workCenterId || !plannedQuantity) return;
    setSaving(true);
    setError(null);
    try {
      const order = await createProductionOrder(tenantCode, {
        bomId: selectedBomId,
        workCenterId,
        plannedQuantity: Number(plannedQuantity),
      });
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
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {error && <InlineError message={error} marginBottom={0} />}

          {/* BOM selector — product is derived from BOM */}
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
              Selecione a BOM (estrutura do produto a produzir)
            </Typography>
            {bomsLoading ? (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, py: 1 }}>
                <CircularProgress size={16} />
                <Typography variant="caption" color="text.secondary">Carregando BOMs ativas…</Typography>
              </Box>
            ) : boms.length === 0 ? (
              <Alert severity="warning" sx={{ py: 0.5 }}>
                Nenhuma BOM ativa encontrada. Ative uma BOM antes de criar a ordem.
              </Alert>
            ) : (
              <FormControl size="small" fullWidth>
                <InputLabel>BOM</InputLabel>
                <Select
                  label="BOM"
                  value={selectedBomId}
                  onChange={e => setSelectedBomId(e.target.value)}
                >
                  {boms.map(b => (
                    <MenuItem key={b.id} value={b.id}>
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>
                          {b.productCode}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          v{b.version} · {b.items.length} componente{b.items.length !== 1 ? 's' : ''}
                        </Typography>
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Box>

          {/* BOM summary — shown after selection */}
          {selectedBom && (
            <Box sx={{ bgcolor: '#f5f5f5', borderRadius: 1.5, p: 1.5 }}>
              <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase', display: 'block', mb: 0.75 }}>
                Materiais da BOM
              </Typography>
              <Stack spacing={0.25}>
                {selectedBom.items.map(item => (
                  <Box key={item.id} sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="caption" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                      {item.materialCode}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {item.quantity} {item.unitOfMeasure}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            </Box>
          )}

          <Stack direction="row" spacing={2}>
            <FormControl size="small" sx={{ flexGrow: 1 }}>
              <InputLabel>Centro de Trabalho</InputLabel>
              <Select
                label="Centro de Trabalho"
                value={workCenterId}
                onChange={e => setWorkCenterId(e.target.value)}
              >
                {activeWorkCenters.map(wc => (
                  <MenuItem key={wc.id} value={wc.id}>{wc.name}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Qtd Planejada"
              type="number"
              size="small"
              sx={{ width: 140 }}
              value={plannedQuantity}
              onChange={e => setPlannedQuantity(e.target.value)}
              slotProps={{ htmlInput: { min: 1 } }}
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
