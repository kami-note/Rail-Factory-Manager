import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Tab,
  Tabs,
  Typography,
  alpha,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  RefreshCw as RefreshIcon,
  Search as SearchIcon,
  Package,
  PackageCheck,
  Plus,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { InventoryBalance } from '../types';
import { InlineError } from '../../../shared/components/common/InlineError';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { BalanceDetailsModal } from './BalanceDetailsModal';
import { useInventoryBalances } from '../hooks/useInventoryBalances';
import { Authorized } from '../../auth';
import { CreateMaterialModal } from './CreateMaterialModal';
import { InventoryDesktopTable, InventoryMobileList } from './InventoryBalanceTable';

interface InventoryStocksPageProps {
  tenantCode: string;
}

const statusFilterOptions = [
  { key: '', label: 'Todos' },
  { key: 'Pending', label: 'Pendente' },
  { key: 'Available', label: 'Disponível' },
  { key: 'Blocked', label: 'Bloqueado' },
];

export function InventoryStocksPage({ tenantCode }: InventoryStocksPageProps) {
  const theme = useTheme();
  const navigate = useNavigate();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  // 0 = Entrada (Purchase/RawMaterial), 1 = Saída (Production/FinishedGood)
  const [tab, setTab] = useState(0);
  const sourceType = tab === 0 ? 'Purchase' : 'Production';

  const { data: balances, loading, error, reload } = useInventoryBalances(tenantCode, sourceType);
  const [statusFilter, setStatusFilter] = useState('');
  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createSuccess, setCreateSuccess] = useState<string | null>(null);

  // Reset status filter when switching tabs
  useEffect(() => { setStatusFilter(''); }, [tab]);

  const allBalances: InventoryBalance[] = balances ?? [];
  const filtered = allBalances.filter(b =>
    statusFilter ? b.status.key === statusFilter : true
  );

  const handleTabChange = (_: React.SyntheticEvent, v: number) => setTab(v);

  const handleMaterialCreated = (code: string) => {
    setShowCreateModal(false);
    setCreateSuccess(`Material "${code}" cadastrado com sucesso.`);
    void reload();
  };

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      <ModuleHeader
        label="ESTOQUE"
        icon={<Package size={20} />}
        action={
          <Authorized permission="inventory.write">
            <Button
              variant="contained"
              size="small"
              startIcon={<Plus size={16} />}
              onClick={() => setShowCreateModal(true)}
            >
              {tab === 0 ? 'Nova Matéria-Prima' : 'Novo Produto Acabado'}
            </Button>
          </Authorized>
        }
      />

      {createSuccess && (
        <Alert severity="success" onClose={() => setCreateSuccess(null)} sx={{ mb: 2 }}>
          {createSuccess}
        </Alert>
      )}
      {error && <InlineError message={error} marginBottom={2} />}

      {/* Tabs */}
      <Tabs
        value={tab}
        onChange={handleTabChange}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
      >
        <Tab
          label="Entrada — Matérias-Primas"
          icon={<Package size={15} />}
          iconPosition="start"
          sx={{ minHeight: 48, fontWeight: 700 }}
        />
        <Tab
          label="Saída — Produtos Acabados"
          icon={<PackageCheck size={15} />}
          iconPosition="start"
          sx={{ minHeight: 48, fontWeight: 700 }}
        />
      </Tabs>

      {/* Filters */}
      <Stack direction="row" spacing={1} sx={{ mb: 2, alignItems: 'center' }}>
        {statusFilterOptions.map(f => (
          <Chip
            key={f.key}
            label={f.label}
            onClick={() => setStatusFilter(f.key)}
            color={statusFilter === f.key ? 'primary' : 'default'}
            variant={statusFilter === f.key ? 'filled' : 'outlined'}
            size="small"
            sx={{ fontWeight: 700 }}
          />
        ))}
        <Box sx={{ flexGrow: 1 }} />
        <Button size="small" startIcon={<RefreshIcon size={14} />} onClick={reload} disabled={loading}>
          Atualizar
        </Button>
      </Stack>

      {/* Content */}
      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : filtered.length === 0 ? (
        <Paper
          variant="outlined"
          sx={{ p: 8, textAlign: 'center', bgcolor: alpha(theme.palette.primary.main, 0.02) }}
        >
          <SearchIcon size={48} style={{ color: theme.palette.text.disabled, marginBottom: 16 }} />
          <Typography variant="h6" color="text.secondary">Nenhum saldo encontrado.</Typography>
          <Typography variant="body2" color="text.disabled">
            {tab === 0
              ? 'Materiais entram via importação de nota fiscal (supply chain).'
              : 'Produtos acabados aparecem aqui após conclusão de ordens de produção.'}
          </Typography>
        </Paper>
      ) : isMobile ? (
        <InventoryMobileList
          balances={filtered}
          onNavigate={code => navigate(`/app/inventory/materials/${code}`)}
          onDetails={id => setSelectedBalanceId(id)}
        />
      ) : (
        <InventoryDesktopTable
          balances={filtered}
          onNavigate={code => navigate(`/app/inventory/materials/${code}`)}
          onDetails={id => setSelectedBalanceId(id)}
        />
      )}

      <BalanceDetailsModal
        balanceId={selectedBalanceId}
        tenantCode={tenantCode}
        onClose={() => setSelectedBalanceId(null)}
      />

      <CreateMaterialModal
        open={showCreateModal}
        tenantCode={tenantCode}
        category={tab === 0 ? 'RawMaterial' : 'FinishedGood'}
        onCreated={handleMaterialCreated}
        onClose={() => setShowCreateModal(false)}
      />
    </Box>
  );
}
