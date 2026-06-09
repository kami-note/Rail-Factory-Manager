import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { Calculator } from 'lucide-react';
import { getBomCostRollup, type BomCostRollup } from '../../api/production';
import { InlineError } from '../../../../shared/components/common/InlineError';
import { toUiErrorMessage } from '../../../../shared/lib/http';

type BomCostRollupModalProps = {
  open: boolean;
  tenantCode: string;
  bomId: string | null;
  productCode: string;
  version: number;
  onClose: () => void;
};

export function BomCostRollupModal({
  open,
  tenantCode,
  bomId,
  productCode,
  version,
  onClose,
}: BomCostRollupModalProps) {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<BomCostRollup | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open && bomId) {
      setLoading(true);
      setError(null);
      setData(null);
      getBomCostRollup(tenantCode, bomId)
        .then(res => {
          setData(res);
        })
        .catch(err => {
          setError(toUiErrorMessage(err, 'Erro ao carregar simulação de custos.'));
        })
        .finally(() => {
          setLoading(false);
        });
    }
  }, [open, tenantCode, bomId]);

  const formatCurrency = (val: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val);

  const formatNumber = (val: number, decimals = 4) => {
    const formatted = new Intl.NumberFormat('pt-BR', {
      minimumFractionDigits: 0,
      maximumFractionDigits: decimals,
    }).format(val);
    return formatted;
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1, fontWeight: 800 }}>
        <Calculator size={20} />
        Simulação de Custo Teórico (Costing Roll-up)
      </DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Custo estimado de materiais para produzir <strong>1 unidade</strong> do produto final{' '}
          <code>{productCode}</code> (v{version}), baseado no preço de compra mais recente registrado nas NF-es de entrada.
        </Typography>

        {loading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        )}

        {error && <InlineError message={error} marginBottom={2} />}

        {!loading && data && (
          <Stack spacing={3}>
            {/* KPI Cards */}
            <Stack direction="row" spacing={2}>
              <Paper
                variant="outlined"
                sx={{
                  p: 2,
                  flexGrow: 1,
                  bgcolor: 'primary.50',
                  borderColor: 'primary.200',
                  borderRadius: 2,
                }}
              >
                <Typography variant="caption" color="primary.800" sx={{ fontWeight: 800 }}>
                  CUSTO TEÓRICO UNITÁRIO
                </Typography>
                <Typography variant="h4" color="primary.900" sx={{ fontWeight: 900, mt: 0.5 }}>
                  {formatCurrency(data.totalEstimatedCost)}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{
                  p: 2,
                  width: 180,
                  bgcolor: 'grey.50',
                  borderRadius: 2,
                }}
              >
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>
                  LOTE PADRÃO DA BOM
                </Typography>
                <Typography variant="h5" sx={{ fontWeight: 800, mt: 0.5 }}>
                  {formatNumber(data.batchSize, 2)}
                </Typography>
              </Paper>
            </Stack>

            <Divider />

            {/* Components list */}
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1 }}>
                Detalhamento dos Componentes
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ bgcolor: 'grey.50' }}>
                      <TableCell sx={{ fontWeight: 800 }}>Componente</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>Qtd. na BOM</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>Perda (%)</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>Qtd. p/ 1 Unid.</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>U.M.</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>Último Preço (NF-e)</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>Custo Parcial</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {data.items.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={7} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                          Nenhum componente cadastrado nesta BOM.
                        </TableCell>
                      </TableRow>
                    ) : (
                      data.items.map(item => (
                        <TableRow key={item.materialCode}>
                          <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
                            {item.materialCode}
                          </TableCell>
                          <TableCell align="right">{formatNumber(item.quantity, 2)}</TableCell>
                          <TableCell align="right">
                            {item.scrapFactor > 0 ? `${(item.scrapFactor * 100).toFixed(2).replace(/\.?0+$/, '')}%` : '0%'}
                          </TableCell>
                          <TableCell align="right" sx={{ fontWeight: 600 }}>
                            {formatNumber(item.scaledQuantity, 4)}
                          </TableCell>
                          <TableCell align="right" sx={{ color: 'text.secondary' }}>
                            {item.unitOfMeasure}
                          </TableCell>
                          <TableCell align="right">
                            {item.unitPrice > 0 ? formatCurrency(item.unitPrice) : <span style={{ color: 'gray', fontStyle: 'italic' }}>Não cotado</span>}
                          </TableCell>
                          <TableCell align="right" sx={{ fontWeight: 700 }}>
                            {formatCurrency(item.totalCost)}
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          </Stack>
        )}
      </DialogContent>
      <DialogActions sx={{ p: 2 }}>
        <Button onClick={onClose} variant="outlined">
          Fechar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
