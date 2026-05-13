import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  CircularProgress,
  Alert,
  Paper,
  IconButton,
  Tooltip,
  Stack,
  Button,
  useMediaQuery,
  useTheme
} from '@mui/material';
import { 
  FileText as DescriptionIcon, 
  Play as PlayArrowIcon, 
  CheckCircle2 as CheckCircleIcon, 
  Info as InfoOutlinedIcon,
  GitPullRequest as ResolveIcon
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { Receipt } from '../types';
import { ReceiptDetailsModal } from './ReceiptDetailsModal';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { StatusChip } from '../../../shared/components/common/StatusChip';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
  onStartConference?: (receiptId: string) => void;
};

/**
 * List view for Material Receipts with operational actions.
 * @param props - Component properties.
 */
export function ReceiptsList({ tenantCode, refreshKey = 0, onStartConference }: ReceiptsListProps) {
  const theme = useTheme();
  const navigate = useNavigate();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);

  const startConference = async (receiptId: string) => {
    try {
      await fetchJsonOrThrow(
        `/api/supply-chain/receipts/${receiptId}/conference/start`,
        {
          method: 'POST',
          headers: buildTenantHeaders(tenantCode),
          credentials: 'include'
        },
        'Falha ao iniciar conferência'
      );

      if (onStartConference) {
        onStartConference(receiptId);
      } else {
        void fetchReceipts();
      }
    } catch (err) {
      console.error(err);
      alert('Erro ao iniciar conferência. Verifique se a nota já foi liberada da associação.');
    }
  };

  const viewXml = async (receiptId: string, receiptNumber: string) => {
    try {
      const response = await fetch(`/api/supply-chain/receipts/${receiptId}/xml`, {
        headers: buildTenantHeaders(tenantCode),
        credentials: 'include'
      });

      if (!response.ok) {
        if (response.status === 404) {
          alert('Arquivo XML não encontrado para este recebimento.');
          return;
        }
        throw new Error('Falha ao buscar XML');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${receiptNumber}.xml`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error(err);
      alert('Erro ao baixar XML original.');
    }
  };

  const fetchReceipts = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await fetchJsonOrThrow<Receipt[]>(
        '/api/supply-chain/receipts',
        {
          headers: buildTenantHeaders(tenantCode),
          credentials: 'include'
        },
        'Falha ao requisitar recebimentos'
      );
      setReceipts(data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchReceipts();
  }, [tenantCode, refreshKey]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress size={32} />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error" sx={{ my: 2 }}>{error}</Alert>;
  }

  if (receipts.length === 0) {
    return (
      <Box sx={{ p: 8, textAlign: 'center' }}>
        <Typography color="text.secondary">Nenhum recebimento registrado ainda.</Typography>
      </Box>
    );
  }

  return (
    <>
      {isCompact ? (
        <Stack spacing={2}>
          {receipts.map((receipt) => {
            return (
              <Paper key={receipt.id} variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
                <Stack spacing={1.5}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1.5 }}>
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 800 }}>
                        {receipt.receiptNumber}
                      </Typography>
                      <Tooltip title="Clique para copiar número do documento">
                        <Typography
                          variant="caption"
                          sx={{ fontWeight: 600, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.documentNumber)}
                        >
                          {receipt.documentNumber}
                        </Typography>
                      </Tooltip>
                    </Box>
                    <StatusChip status={receipt.status} />
                  </Box>

                  {receipt.accessKey && (
                    <Tooltip title="Clique para copiar chave de acesso completa">
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                        onClick={() => receipt.accessKey && TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
                      >
                        Chave: {TechnicalIdFormatter.truncate(receipt.accessKey)}...
                      </Typography>
                    </Tooltip>
                  )}

                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                    <Typography variant="caption" color="text.secondary">Itens: <strong>{receipt.itemCount}</strong></Typography>
                    <Typography variant="caption" color="text.secondary">
                      Total: <strong>{receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}</strong>
                    </Typography>
                  </Box>

                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                    <Button size="small" variant="outlined" startIcon={<InfoOutlinedIcon size={14} />} onClick={() => setSelectedReceiptId(receipt.id)}>
                      Detalhes
                    </Button>
                    {receipt.status.key === 'PendingAssociation' && (
                      <Button 
                        size="small" 
                        variant="contained" 
                        color="warning" 
                        startIcon={<ResolveIcon size={14} />} 
                        onClick={() => navigate(`/app/supply-chain/association?receiptId=${receipt.id}`)}
                      >
                        Resolver
                      </Button>
                    )}
                    {receipt.status.key === 'Registered' && (
                      <Button size="small" variant="outlined" color="success" startIcon={<PlayArrowIcon size={14} />} onClick={() => startConference(receipt.id)}>
                        Iniciar
                      </Button>
                    )}
                    {receipt.status.key === 'InConference' && (
                      <Button size="small" variant="outlined" color="warning" startIcon={<CheckCircleIcon size={14} />} onClick={() => onStartConference && onStartConference(receipt.id)}>
                        Contar
                      </Button>
                    )}
                    <Button size="small" variant="outlined" color="primary" startIcon={<DescriptionIcon size={14} />} onClick={() => viewXml(receipt.id, receipt.receiptNumber)}>
                      XML
                    </Button>
                  </Stack>
                </Stack>
              </Paper>
            );
          })}
        </Stack>
      ) : (
        <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto', borderRadius: 2 }}>
          <Table sx={{ minWidth: 760 }} size="small">
            <TableHead>
              <TableRow sx={{ bgcolor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 800 }}>NOTA FISCAL</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>DOCUMENTO / CHAVE</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>VALOR TOTAL</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>ITENS</TableCell>
                <TableCell sx={{ fontWeight: 800 }} align="right">AÇÕES</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {receipts.map((receipt) => {
                return (
                  <TableRow key={receipt.id} hover>
                    <TableCell sx={{ fontWeight: 700, color: 'primary.main' }}>{receipt.receiptNumber}</TableCell>
                    <TableCell>
                      <Tooltip title="Clique para copiar número do documento">
                        <Typography
                          variant="body2"
                          sx={{ fontWeight: 600, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.documentNumber)}
                        >
                          {receipt.documentNumber}
                        </Typography>
                      </Tooltip>
                      {receipt.accessKey && (
                        <Tooltip title="Clique para copiar chave de acesso completa">
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', '&:hover': { textDecoration: 'underline' }, mt: 0.5 }}
                            onClick={() => receipt.accessKey && TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
                          >
                            {TechnicalIdFormatter.truncate(receipt.accessKey)}...
                          </Typography>
                        </Tooltip>
                      )}
                    </TableCell>
                    <TableCell>
                      <StatusChip status={receipt.status} />
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>
                      {receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{receipt.itemCount}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="Ver Detalhes Completos">
                        <IconButton
                          size="small"
                          color="info"
                          onClick={() => setSelectedReceiptId(receipt.id)}
                        >
                          <InfoOutlinedIcon size={16} />
                        </IconButton>
                      </Tooltip>
                      {receipt.status.key === 'PendingAssociation' && (
                        <Tooltip title="Resolver SKUs na Bancada">
                          <IconButton
                            size="small"
                            color="warning"
                            onClick={() => navigate(`/app/supply-chain/association?receiptId=${receipt.id}`)}
                          >
                            <ResolveIcon size={16} />
                          </IconButton>
                        </Tooltip>
                      )}
                      {receipt.status.key === 'Registered' && (
                        <Tooltip title="Iniciar Conferência Cega">
                          <IconButton
                            size="small"
                            color="success"
                            onClick={() => startConference(receipt.id)}
                          >
                            <PlayArrowIcon size={16} />
                          </IconButton>
                        </Tooltip>
                      )}
                      {receipt.status.key === 'InConference' && (
                        <Tooltip title="Realizar Contagem Física">
                          <IconButton
                            size="small"
                            color="warning"
                            onClick={() => onStartConference && onStartConference(receipt.id)}
                          >
                            <CheckCircleIcon size={16} />
                          </IconButton>
                        </Tooltip>
                      )}
                      <Tooltip title="Baixar XML Original">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => viewXml(receipt.id, receipt.receiptNumber)}
                        >
                          <DescriptionIcon size={16} />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <ReceiptDetailsModal
        receiptId={selectedReceiptId}
        tenantCode={tenantCode}
        onClose={() => setSelectedReceiptId(null)}
      />
    </>
  );
}
