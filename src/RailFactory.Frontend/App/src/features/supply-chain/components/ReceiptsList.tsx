import React, { useEffect, useState, useMemo } from 'react';
import {
  Box,
  Typography,
  CircularProgress,
  IconButton,
  Tooltip,
  Stack,
  Button,
  useTheme,
  Card,
  CardContent,
  CardActions,
  Divider,
  Avatar,
  TextField,
  MenuItem,
  InputAdornment,
  Select,
  FormControl,
  InputLabel
} from '@mui/material';
import { 
  FileText as DescriptionIcon, 
  Play as PlayArrowIcon, 
  CheckCircle2 as CheckCircleIcon, 
  Info as InfoOutlinedIcon,
  GitPullRequest as ResolveIcon,
  Building2,
  Calendar,
  Hash,
  Search as SearchIcon
} from 'lucide-react';
import type { Receipt } from '../types';
import { ReceiptDetailsModal } from './ReceiptDetailsModal';
import { TechnicalIdFormatter, CurrencyFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow, readProblemMessage, toUiErrorMessage } from '../../../shared/lib/http';
import { InlineError } from '../../../shared/components/common/InlineError';
import { StatusChip } from '../../../shared/components/common/StatusChip';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
  onStartConference?: (receiptId: string) => void;
  onStartAssociation?: (receiptId: string) => void;
};

/**
 * ReceiptsList is responsible for fetching and rendering the grid of receipt cards.
 * Refactored using Leaf Node Pattern to ensure internal modularity.
 */
export function ReceiptsList({ tenantCode, refreshKey = 0, onStartConference, onStartAssociation }: ReceiptsListProps) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);

  // Filters State
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');

  const fetchReceipts = async () => {
    setLoading(true);
    setLoadError(null);

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
      setLoadError(toUiErrorMessage(requestError, 'Não foi possível carregar os recebimentos.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchReceipts();
  }, [tenantCode, refreshKey]);

  const startConference = async (receiptId: string) => {
    try {
      setActionError(null);
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
      setActionError(toUiErrorMessage(err, 'Não foi possível iniciar a conferência deste recebimento.'));
    }
  };

  const viewXml = async (receiptId: string, receiptNumber: string) => {
    try {
      setActionError(null);
      const response = await fetch(`/api/supply-chain/receipts/${receiptId}/xml`, {
        headers: buildTenantHeaders(tenantCode),
        credentials: 'include'
      });

      if (!response.ok) {
        const message = await readProblemMessage(response, 'Não foi possível baixar o XML deste recebimento.');
        throw new Error(message);
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
      setActionError(toUiErrorMessage(err, 'Não foi possível baixar o XML deste recebimento.'));
    }
  };

  // Memoized filtering logic
  const filteredReceipts = useMemo(() => {
    return receipts.filter((r) => {
      // Search logic (Number, Supplier, Access Key)
      const query = searchQuery.toLowerCase().trim();
      const matchesSearch = query === '' 
        || r.receiptNumber.toLowerCase().includes(query)
        || r.supplierName.toLowerCase().includes(query)
        || r.documentNumber?.toLowerCase().includes(query)
        || r.accessKey?.toLowerCase().includes(query);

      // Status logic
      const matchesStatus = statusFilter === 'ALL' || r.status.key === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [receipts, searchQuery, statusFilter]);

  // Extract unique statuses from receipts for the filter dropdown
  const availableStatuses = useMemo(() => {
    const statusesMap = new Map<string, string>();
    receipts.forEach(r => {
      if (!statusesMap.has(r.status.key)) {
        statusesMap.set(r.status.key, r.status.label);
      }
    });
    return Array.from(statusesMap.entries()).map(([key, label]) => ({ key, label }));
  }, [receipts]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress size={32} />
      </Box>
    );
  }

  if (loadError) {
    return <InlineError message={loadError} marginBottom={2} />;
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
      {actionError && (
        <InlineError message={actionError} onClose={() => setActionError(null)} marginBottom={2} />
      )}
      
      <ReceiptsToolbar 
        searchQuery={searchQuery}
        setSearchQuery={setSearchQuery}
        statusFilter={statusFilter}
        setStatusFilter={setStatusFilter}
        availableStatuses={availableStatuses}
      />

      {filteredReceipts.length === 0 ? (
        <Box sx={{ p: 6, textAlign: 'center', bgcolor: 'background.paper', borderRadius: 3, border: '1px solid', borderColor: 'divider' }}>
          <Typography variant="h6" color="text.primary" sx={{ fontWeight: 700, mb: 1 }}>Nenhum resultado encontrado</Typography>
          <Typography color="text.secondary">Tente ajustar seus termos de pesquisa ou filtros.</Typography>
        </Box>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr' }, gap: 3 }}>
          {filteredReceipts.map((receipt) => (
            <ReceiptCard 
              key={receipt.id}
              receipt={receipt}
              onStartConference={() => startConference(receipt.id)}
              onContinueConference={() => onStartConference && onStartConference(receipt.id)}
              onViewDetails={() => setSelectedReceiptId(receipt.id)}
              onViewXml={() => viewXml(receipt.id, receipt.receiptNumber)}
              onResolveAssociation={() => onStartAssociation && onStartAssociation(receipt.id)}
            />
          ))}
        </Box>
      )}

      <ReceiptDetailsModal
        receiptId={selectedReceiptId}
        tenantCode={tenantCode}
        onClose={() => setSelectedReceiptId(null)}
      />
    </>
  );
}

// ----------------------------------------------------------------------
// LEAF NODE COMPONENTS (Local-First Rule)
// ----------------------------------------------------------------------

function ReceiptsToolbar({
  searchQuery,
  setSearchQuery,
  statusFilter,
  setStatusFilter,
  availableStatuses
}: {
  searchQuery: string;
  setSearchQuery: (val: string) => void;
  statusFilter: string;
  setStatusFilter: (val: string) => void;
  availableStatuses: Array<{ key: string, label: string }>;
}) {
  return (
    <Box sx={{ mb: 3, display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 2 }}>
      <TextField
        placeholder="Pesquisar por fornecedor, número ou chave da NF-e..."
        value={searchQuery}
        onChange={(e) => setSearchQuery(e.target.value)}
        fullWidth
        size="small"
        variant="outlined"
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon size={18} />
              </InputAdornment>
            ),
            sx: { borderRadius: 2, bgcolor: 'background.paper' }
          }
        }}
      />
      <FormControl size="small" sx={{ minWidth: { xs: '100%', sm: 200 } }}>
        <InputLabel id="status-filter-label" sx={{ fontWeight: 600 }}>Filtrar Status</InputLabel>
        <Select
          labelId="status-filter-label"
          label="Filtrar Status"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          sx={{ borderRadius: 2, bgcolor: 'background.paper', fontWeight: 600 }}
        >
          <MenuItem value="ALL" sx={{ fontWeight: 600 }}>Todos os Status</MenuItem>
          {availableStatuses.map((status) => (
            <MenuItem key={status.key} value={status.key} sx={{ fontWeight: 600 }}>
              {status.label}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Box>
  );
}

function ReceiptCard({
  receipt,
  onStartConference,
  onContinueConference,
  onViewDetails,
  onViewXml,
  onResolveAssociation
}: {
  receipt: Receipt;
  onStartConference: () => void;
  onContinueConference: () => void;
  onViewDetails: () => void;
  onViewXml: () => void;
  onResolveAssociation: () => void;
}) {
  const theme = useTheme();
  const issuedDate = new Date(receipt.issuedAt).toLocaleDateString('pt-BR');

  return (
    <Card 
      elevation={0}
      sx={{ 
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderRadius: 3,
        border: '1px solid',
        borderColor: 'divider',
        transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
        '&:hover': {
          borderColor: 'primary.main',
          boxShadow: theme.shadows[4],
          transform: 'translateY(-4px)'
        }
      }}
    >
      <CardContent sx={{ flexGrow: 1, p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <StatusChip status={receipt.status} />
          <Stack direction="row" spacing={0.5} sx={{ color: 'text.secondary', alignItems: 'center' }}>
            <Calendar size={14} />
            <Typography variant="caption" sx={{ fontWeight: 600 }}>
              {issuedDate}
            </Typography>
          </Stack>
        </Box>

        <Stack direction="row" spacing={2} sx={{ mb: 2.5, alignItems: 'center' }}>
          <Avatar sx={{ bgcolor: `${theme.palette.primary.main}15`, color: 'primary.main', width: 40, height: 40, borderRadius: 2 }}>
            <Building2 size={20} />
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 800, lineHeight: 1.2, mb: 0.5 }} noWrap title={receipt.supplierName}>
              {receipt.supplierName}
            </Typography>
            <Stack direction="row" spacing={0.5} sx={{ color: 'text.secondary', alignItems: 'center' }}>
              <Hash size={14} />
              <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                NF {receipt.receiptNumber}
              </Typography>
            </Stack>
          </Box>
        </Stack>

        <Box 
          sx={{ 
            p: 2, 
            borderRadius: 2, 
            bgcolor: theme.palette.mode === 'dark' ? 'background.default' : 'grey.50',
            border: '1px solid',
            borderColor: theme.palette.mode === 'dark' ? 'divider' : 'grey.100',
            mb: 2
          }}
        >
          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                Valor Total
              </Typography>
              <Typography variant="body2" color="text.primary" sx={{ fontWeight: 800 }}>
                {CurrencyFormatter.format(receipt.totalValue)}
              </Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                Volume (Itens)
              </Typography>
              <Typography variant="body2" color="text.primary" sx={{ fontWeight: 800 }}>
                {receipt.itemCount} UN
              </Typography>
            </Box>
          </Box>
        </Box>

        <Box>
          <Tooltip title="Clique para copiar a chave">
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ 
                display: 'block', 
                fontFamily: 'monospace', 
                cursor: 'pointer', 
                '&:hover': { color: 'primary.main' },
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap'
              }}
              onClick={() => receipt.accessKey && TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
            >
              {receipt.accessKey || receipt.documentNumber}
            </Typography>
          </Tooltip>
        </Box>
      </CardContent>

      <Divider />

      <CardActions sx={{ p: 2, pt: 1.5, pb: 1.5, bgcolor: 'background.paper', justifyContent: 'space-between' }}>
        <Button 
          size="small" 
          color="inherit" 
          startIcon={<InfoOutlinedIcon size={16} />} 
          onClick={onViewDetails}
          sx={{ fontWeight: 600 }}
        >
          Detalhes
        </Button>
        
        <Stack direction="row" spacing={1}>
          <Tooltip title="Baixar XML">
            <IconButton 
              size="small" 
              onClick={onViewXml}
              sx={{ color: 'text.secondary', '&:hover': { color: 'primary.main', bgcolor: 'primary.50' } }}
            >
              <DescriptionIcon size={18} />
            </IconButton>
          </Tooltip>

          {receipt.status.key === 'PendingAssociation' && (
            <Button 
              size="small" 
              variant="contained" 
              color="warning" 
              disableElevation
              startIcon={<ResolveIcon size={16} />} 
              onClick={onResolveAssociation}
              sx={{ borderRadius: 2, fontWeight: 700 }}
            >
              Resolver
            </Button>
          )}
          {receipt.status.key === 'Registered' && (
            <Button 
              size="small" 
              variant="contained" 
              color="primary" 
              disableElevation
              startIcon={<PlayArrowIcon size={16} />} 
              onClick={onStartConference}
              sx={{ borderRadius: 2, fontWeight: 700 }}
            >
              Iniciar
            </Button>
          )}
          {receipt.status.key === 'InConference' && (
            <Button 
              size="small" 
              variant="contained" 
              color="success" 
              disableElevation
              startIcon={<CheckCircleIcon size={16} />} 
              onClick={onContinueConference}
              sx={{ borderRadius: 2, fontWeight: 700 }}
            >
              Contar
            </Button>
          )}
        </Stack>
      </CardActions>
    </Card>
  );
}
