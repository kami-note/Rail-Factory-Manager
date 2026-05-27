import React from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { RefreshCw } from 'lucide-react';
import type { OrderExecutionHistory } from '../../types';

export function ExecutionHistory({ history, loading, error, onRefresh }: {
  history: OrderExecutionHistory | null;
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}) {
  if (loading) return <Box sx={{ textAlign: 'center', py: 4 }}><CircularProgress size={24} /></Box>;
  if (error) return <Alert severity="error" action={<Button size="small" onClick={onRefresh}>Tentar novamente</Button>}>{error}</Alert>;
  if (!history) return null;

  const isEmpty = history.consumptions.length === 0 && history.scraps.length === 0 && history.inspections.length === 0;

  if (isEmpty) return (
    <Box sx={{ textAlign: 'center', py: 4 }}>
      <Typography variant="body2" color="text.secondary">Nenhum registro de execução.</Typography>
    </Box>
  );

  return (
    <Stack spacing={3}>
      {history.consumptions.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'primary.main', display: 'block', mb: 1 }}>
            CONSUMOS ({history.consumptions.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.consumptions.map((c, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{c.materialCode}</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{c.consumedQuantity} {c.unitOfMeasure}</Typography>
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {new Date(c.recordedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      {history.scraps.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'warning.main', display: 'block', mb: 1 }}>
            SCRAP ({history.scraps.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.scraps.map((s, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{s.materialCode}</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{s.scrapQuantity} {s.unitOfMeasure}</Typography>
                </Stack>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{s.reason}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {new Date(s.recordedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      {history.inspections.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1 }}>
            INSPEÇÕES ({history.inspections.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.inspections.map((ins, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5, borderColor: ins.result === 'Passed' ? 'success.light' : 'error.light' }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Chip
                    size="small"
                    label={ins.result === 'Passed' ? 'Aprovado' : 'Reprovado'}
                    color={ins.result === 'Passed' ? 'success' : 'error'}
                    variant="filled"
                    sx={{ height: 20, fontSize: '0.65rem' }}
                  />
                  <Typography variant="caption" color="text.secondary">{ins.inspectedBy}</Typography>
                </Stack>
                {ins.notes && <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>{ins.notes}</Typography>}
                <Typography variant="caption" color="text.disabled">
                  {new Date(ins.inspectedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      <Button size="small" startIcon={<RefreshCw size={13} />} onClick={onRefresh} sx={{ alignSelf: 'flex-end' }}>
        Atualizar
      </Button>
    </Stack>
  );
}
