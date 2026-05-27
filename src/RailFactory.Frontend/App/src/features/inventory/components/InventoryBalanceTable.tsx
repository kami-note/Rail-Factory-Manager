import React from 'react';
import {
  Box,
  Button,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import { Info as InfoIcon } from 'lucide-react';
import type { InventoryBalance } from '../types';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';

const formatDate = (dateIso: string, includeTime = true) => {
  if (!dateIso) return '-';
  return new Date(dateIso).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    ...(includeTime ? { hour: '2-digit', minute: '2-digit' } : {}),
  });
};

export function InventoryDesktopTable({ balances, onNavigate, onDetails }: {
  balances: InventoryBalance[];
  onNavigate: (code: string) => void;
  onDetails: (id: string) => void;
}) {
  return (
    <TableContainer component={Paper} variant="outlined">
      <Table stickyHeader size="small">
        <TableHead>
          <TableRow>
            <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>LOTE / VALIDADE</TableCell>
            <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>UNIDADE</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>ORIGEM</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>CRIADO EM</TableCell>
            <TableCell align="right" sx={{ fontWeight: 800 }}>AÇÕES</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {balances.map(b => (
            <TableRow key={b.id} hover>
              <TableCell>
                <Box
                  sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }}
                  onClick={() => onNavigate(b.materialCode)}
                >
                  <MaterialAvatar materialCode={b.materialCode} size={32} description={b.materialName} imageUrl={b.materialImageUrl} />
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main', '&:hover': { textDecoration: 'underline' } }}>
                      {b.materialName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                      {b.materialCode}
                    </Typography>
                  </Box>
                </Box>
              </TableCell>
              <TableCell>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>{b.lotNumber || 'N/A'}</Typography>
                {b.expirationDate && (
                  <Typography variant="caption" color="text.secondary">
                    {new Date(b.expirationDate).toLocaleDateString('pt-BR')}
                  </Typography>
                )}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>{b.quantity}</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>{b.unitOfMeasure}</TableCell>
              <TableCell><StatusChip status={b.status} /></TableCell>
              <TableCell>
                <StatusChip status={b.sourceType} label={b.supplierName || b.sourceType.label} />
                <Tooltip title="Clique para copiar referência">
                  <Typography
                    variant="caption"
                    color="text.disabled"
                    sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', mt: 0.5 }}
                    onClick={() => TechnicalIdFormatter.copyToClipboard(b.sourceReference)}
                  >
                    {TechnicalIdFormatter.truncate(b.sourceReference)}
                  </Typography>
                </Tooltip>
              </TableCell>
              <TableCell>{formatDate(b.createdAt)}</TableCell>
              <TableCell align="right">
                <Tooltip title="Ver detalhes e histórico">
                  <IconButton size="small" onClick={() => onDetails(b.id)}>
                    <InfoIcon size={16} />
                  </IconButton>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function InventoryMobileList({ balances, onNavigate, onDetails }: {
  balances: InventoryBalance[];
  onNavigate: (code: string) => void;
  onDetails: (id: string) => void;
}) {
  return (
    <Stack spacing={2}>
      {balances.map(b => (
        <Paper key={b.id} variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
          <Stack spacing={2}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <Box
                sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }}
                onClick={() => onNavigate(b.materialCode)}
              >
                <MaterialAvatar materialCode={b.materialCode} size={40} description={b.materialName} imageUrl={b.materialImageUrl} />
                <Box>
                  <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>{b.materialName}</Typography>
                  <Typography variant="caption" color="text.secondary">{b.materialCode}</Typography>
                </Box>
              </Box>
              <StatusChip status={b.status} />
            </Box>
            <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
              <Typography variant="caption" color="text.secondary">Qtd: <strong>{b.quantity}</strong> {b.unitOfMeasure}</Typography>
              <Typography variant="caption" color="text.secondary">Lote: <strong>{b.lotNumber || 'N/A'}</strong></Typography>
              <Typography variant="caption" color="text.secondary">Origem: <StatusChip status={b.sourceType} label={b.supplierName || b.sourceType.label} /></Typography>
              <Typography variant="caption" color="text.secondary">Criado: <strong>{formatDate(b.createdAt)}</strong></Typography>
            </Box>
            <Button size="small" variant="outlined" startIcon={<InfoIcon size={14} />} onClick={() => onDetails(b.id)}>
              Ver Histórico Completo
            </Button>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}
