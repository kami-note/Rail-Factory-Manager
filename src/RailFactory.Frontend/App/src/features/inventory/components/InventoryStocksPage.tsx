import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  CircularProgress,
  Paper,
  Stack,
  Button,
  IconButton,
  Tooltip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  useTheme,
  useMediaQuery,
  alpha
} from '@mui/material';
import {
  RefreshCw as RefreshIcon,
  Search as SearchIcon,
  Info as InfoIcon,
  ExternalLink as LaunchIcon
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { InventoryBalance } from '../types';
import { InlineError } from '../../../shared/components/common/InlineError';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { BalanceDetailsModal } from './BalanceDetailsModal';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';

interface InventoryStocksPageProps {
  tenantCode: string;
}

const formatRelativeDate = (dateIso: string, includeTime = true) => {
  if (!dateIso) return '-';
  const date = new Date(dateIso);
  return date.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    ...(includeTime ? { hour: '2-digit', minute: '2-digit' } : {})
  });
};

/**
 * List view for current inventory balances.
 * @param props - Component properties.
 * @remarks
 * Localization: All operator-facing labels are in Portuguese (Brazil).
 * Standard: Uses StatusChip for all status rendering.
 */
export function InventoryStocksPage({ tenantCode }: InventoryStocksPageProps) {
  const theme = useTheme();
  const navigate = useNavigate();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [balances, setBalances] = useState<InventoryBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [filter, setFilter] = useState<string>('Todos');

  const filterOptions = [
    { key: 'All', label: 'Todos' },
    { key: 'Pending', label: 'Pendente' },
    { key: 'Available', label: 'Disponível' },
    { key: 'Blocked', label: 'Bloqueado' }
  ];

  const loadBalances = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchJsonOrThrow<InventoryBalance[]>(
        '/api/inventory/balances',
        {
          headers: buildTenantHeaders(tenantCode),
          credentials: 'include'
        },
        'Falha ao carregar saldos de estoque'
      );
      setBalances(data);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar os saldos de estoque.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadBalances();
  }, [tenantCode]);

  const handleNavigateToMaterial = (code: string) => {
    navigate(`/app/inventory/materials/${code}`);
  };

  const filteredBalances = balances.filter(b => {
    if (filter === 'Todos') return true;
    const option = filterOptions.find(o => o.label === filter);
    return b.status.key === option?.key;
  });

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      <ModuleHeader 
        label="SALDOS DE ESTOQUE" 
        icon={<LaunchIcon size={20} />}
        action={
          <Button
            variant="outlined"
            startIcon={<RefreshIcon size={16} />}
            onClick={loadBalances}
            disabled={loading}
            size="small"
          >
            Atualizar
          </Button>
        }
      />

      <Stack direction="row" spacing={1} sx={{ mt: 2, mb: 3 }}>
        {filterOptions.map((f) => (
          <Chip
            key={f.key}
            label={f.label}
            onClick={() => setFilter(f.label)}
            color={filter === f.label ? 'primary' : 'default'}
            variant={filter === f.label ? 'filled' : 'outlined'}
            size="small"
            sx={{ fontWeight: 700 }}
          />
        ))}
      </Stack>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>
      ) : error ? (
        <InlineError message={error} marginBottom={3} />
      ) : filteredBalances.length === 0 ? (
        <Paper variant="outlined" sx={{ p: 8, textAlign: 'center', bgcolor: alpha(theme.palette.primary.main, 0.02) }}>
          <SearchIcon size={48} style={{ color: theme.palette.text.disabled, marginBottom: 16 }} />
          <Typography variant="h6" color="text.secondary">Nenhum saldo encontrado.</Typography>
          <Typography variant="body2" color="text.disabled">Tente mudar seus filtros ou aguarde novos recebimentos.</Typography>
        </Paper>
      ) : isMobile ? (
        <Stack spacing={2}>
          {filteredBalances.map((balance) => (
            <Paper key={balance.id} variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
              <Stack spacing={2}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }} onClick={() => handleNavigateToMaterial(balance.materialCode)}>
                    <MaterialAvatar materialCode={balance.materialCode} size={40} description={balance.materialName} imageUrl={balance.materialImageUrl} />
                    <Box>
                      <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>{balance.materialName}</Typography>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>SKU: {balance.materialCode}</Typography>
                    </Box>
                  </Box>
                  <StatusChip status={balance.status} />
                </Box>

                <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                  <Typography variant="caption" color="text.secondary">Qtd: <strong>{balance.quantity}</strong> {balance.unitOfMeasure}</Typography>
                  <Typography variant="caption" color="text.secondary">Lote: <strong>{balance.lotNumber || 'N/A'}</strong></Typography>
                  <Typography variant="caption" color="text.secondary">Origem: <StatusChip status={balance.sourceType} label={balance.supplierName || balance.sourceType.label} /></Typography>
                  <Typography variant="caption" color="text.secondary">Criado: <strong>{formatRelativeDate(balance.createdAt)}</strong></Typography>
                </Box>

                <Button size="small" variant="outlined" startIcon={<InfoIcon size={14} />} onClick={() => setSelectedBalanceId(balance.id)}>
                  Ver Histórico Completo
                </Button>
              </Stack>
            </Paper>
          ))}
        </Stack>
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow sx={{ bgcolor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>LOTE / VALIDADE</TableCell>
                <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>UNIDADE</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>ORIGEM / FORNECEDOR</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>CRIADO EM</TableCell>
                <TableCell align="right" sx={{ fontWeight: 800 }}>AÇÕES</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredBalances.map((balance) => (
                <TableRow key={balance.id} hover>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }} onClick={() => handleNavigateToMaterial(balance.materialCode)}>
                      <MaterialAvatar materialCode={balance.materialCode} size={32} description={balance.materialName} imageUrl={balance.materialImageUrl} />
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main', '&:hover': { textDecoration: 'underline' } }}>{balance.materialName}</Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontWeight: 600, fontFamily: 'monospace' }}>{balance.materialCode}</Typography>
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{balance.lotNumber || 'N/A'}</Typography>
                    {balance.expirationDate && <Typography variant="caption" color="text.secondary">{formatRelativeDate(balance.expirationDate, false)}</Typography>}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 800 }}>{balance.quantity}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{balance.unitOfMeasure}</TableCell>
                  <TableCell><StatusChip status={balance.status} /></TableCell>
                  <TableCell>
                    <StatusChip status={balance.sourceType} label={balance.supplierName || balance.sourceType.label} />
                    <Tooltip title="Clique para copiar referência">
                      <Typography 
                        variant="caption" 
                        color="text.disabled" 
                        sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', mt: 0.5 }}
                        onClick={() => TechnicalIdFormatter.copyToClipboard(balance.sourceReference)}
                      >
                        {TechnicalIdFormatter.truncate(balance.sourceReference)}
                      </Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell>{formatRelativeDate(balance.createdAt)}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="Ver detalhes e histórico">
                      <IconButton size="small" onClick={() => setSelectedBalanceId(balance.id)}>
                        <InfoIcon size={16} />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <BalanceDetailsModal
        balanceId={selectedBalanceId}
        tenantCode={tenantCode}
        onClose={() => setSelectedBalanceId(null)}
      />
    </Box>
  );
}
