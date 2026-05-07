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
  Chip,
  Stack,
  Button,
  useMediaQuery,
  useTheme
} from '@mui/material';
import DescriptionIcon from '@mui/icons-material/Description';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import type { Receipt } from '../types';
import { ReceiptDetailsModal } from './ReceiptDetailsModal';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
  onStartConference?: (receiptId: string) => void;
};

export function ReceiptsList({ tenantCode, refreshKey = 0, onStartConference }: ReceiptsListProps) {
  const theme = useTheme();
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
        'Failed to start conference'
      );

      if (onStartConference) {
        onStartConference(receiptId);
      } else {
        fetchReceipts();
      }
    } catch (err) {
      console.error(err);
      alert('Error starting conference.');
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
          alert('XML not found for this receipt.');
          return;
        }
        throw new Error('Failed to fetch XML');
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
      alert('Error downloading XML.');
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
        'Receipts request failed'
      );
      setReceipts(data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unknown error');
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
      <Box sx={{ p: 4, textAlign: 'center' }}>
        <Typography color="text.secondary">No receipts yet.</Typography>
      </Box>
    );
  }

  return (
    <>
      {isCompact ? (
        <Stack spacing={2}>
          {receipts.map((receipt) => {
            const status = receipt.status;
            return (
              <Paper key={receipt.id} variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1.5}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1.5 }}>
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 800 }}>
                        {receipt.receiptNumber}
                      </Typography>
                      <Tooltip title="Click to copy document number">
                        <Typography
                          variant="caption"
                          sx={{ fontWeight: 600, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.documentNumber)}
                        >
                          {receipt.documentNumber}
                        </Typography>
                      </Tooltip>
                    </Box>
                    <Chip size="small" label={status.label} color={status.color as any} variant="outlined" sx={{ fontWeight: 700 }} />
                  </Box>

                  {receipt.accessKey && (
                    <Tooltip title="Click to copy full access key">
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                        onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
                      >
                        Access key: {TechnicalIdFormatter.truncate(receipt.accessKey)}...
                      </Typography>
                    </Tooltip>
                  )}

                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                    <Typography variant="caption" color="text.secondary">Items: <strong>{receipt.itemCount}</strong></Typography>
                    <Typography variant="caption" color="text.secondary">
                      Total: <strong>{receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}</strong>
                    </Typography>
                  </Box>

                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                    <Button size="small" variant="outlined" startIcon={<InfoOutlinedIcon fontSize="small" />} onClick={() => setSelectedReceiptId(receipt.id)}>
                      Details
                    </Button>
                    {receipt.status === 'Registered' && (
                      <Button size="small" variant="outlined" color="success" startIcon={<PlayArrowIcon fontSize="small" />} onClick={() => startConference(receipt.id)}>
                        Start
                      </Button>
                    )}
                    {receipt.status === 'InConference' && (
                      <Button size="small" variant="outlined" color="warning" startIcon={<CheckCircleIcon fontSize="small" />} onClick={() => onStartConference && onStartConference(receipt.id)}>
                        Count
                      </Button>
                    )}
                    <Button size="small" variant="outlined" color="primary" startIcon={<DescriptionIcon fontSize="small" />} onClick={() => viewXml(receipt.id, receipt.receiptNumber)}>
                      XML
                    </Button>
                  </Stack>
                </Stack>
              </Paper>
            );
          })}
        </Stack>
      ) : (
        <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
          <Table sx={{ minWidth: 760 }}>
            <TableHead>
              <TableRow sx={{ bgcolor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 700 }}>Receipt</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Document / Access Key</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Total</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Items</TableCell>
                <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {receipts.map((receipt) => {
                const status = receipt.status;
                return (
                  <TableRow key={receipt.id} hover>
                    <TableCell sx={{ fontWeight: 600 }}>{receipt.receiptNumber}</TableCell>
                    <TableCell>
                      <Tooltip title="Click to copy document number">
                        <Typography
                          variant="body2"
                          sx={{ fontWeight: 500, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.documentNumber)}
                        >
                          {receipt.documentNumber}
                        </Typography>
                      </Tooltip>
                      {receipt.accessKey && (
                        <Tooltip title="Click to copy full access key">
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                            onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
                          >
                            {TechnicalIdFormatter.truncate(receipt.accessKey)}...
                          </Typography>
                        </Tooltip>
                      )}
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={status.label}
                        color={status.color as any}
                        variant="outlined"
                        sx={{ fontWeight: 700 }}
                      />
                    </TableCell>
                    <TableCell>
                      {receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}
                    </TableCell>
                    <TableCell>{receipt.itemCount}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="View Full Details">
                        <IconButton
                          size="small"
                          color="info"
                          onClick={() => setSelectedReceiptId(receipt.id)}
                        >
                          <InfoOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      {receipt.status === 'Registered' && (
                        <Tooltip title="Start Conference">
                          <IconButton
                            size="small"
                            color="success"
                            onClick={() => startConference(receipt.id)}
                          >
                            <PlayArrowIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                      {receipt.status === 'InConference' && (
                        <Tooltip title="Count Items">
                          <IconButton
                            size="small"
                            color="warning"
                            onClick={() => onStartConference && onStartConference(receipt.id)}
                          >
                            <CheckCircleIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                      <Tooltip title="Download original XML">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => viewXml(receipt.id, receipt.receiptNumber)}
                        >
                          <DescriptionIcon fontSize="small" />
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
