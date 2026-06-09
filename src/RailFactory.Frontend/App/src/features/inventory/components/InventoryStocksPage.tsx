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
  TextField,
  InputAdornment,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  FormControlLabel,
  Checkbox,
  IconButton,
  Grid,
  ToggleButton,
  ToggleButtonGroup,
  Divider,
} from '@mui/material';
import {
  RefreshCw as RefreshIcon,
  Search as SearchIcon,
  Package,
  PackageCheck,
  Plus,
  Lock,
  FilterX,
  X,
  LayoutGrid,
  List,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { InventoryBalance } from '../types';
import { InlineError } from '../../../shared/components/common/InlineError';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { BalanceDetailsModal } from './BalanceDetailsModal';
import { useInventoryBalances } from '../hooks/useInventoryBalances';
import { Authorized } from '../../auth';
import { CreateMaterialModal } from './CreateMaterialModal';
import { InventoryDesktopTable, InventoryMobileList, InventoryBalanceCardList } from './InventoryBalanceTable';
import { StatCard } from '../../../shared/components/common/StatCard';

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
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState('name-asc');
  const [hideZeroStock, setHideZeroStock] = useState(false);
  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createSuccess, setCreateSuccess] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'grid' | 'table'>(() => {
    return (localStorage.getItem('inventory_view_mode') as 'grid' | 'table') || 'grid';
  });

  // Reset filters when switching tabs
  useEffect(() => {
    setStatusFilter('');
    setSearchQuery('');
    setHideZeroStock(false);
    setSortBy('name-asc');
  }, [tab]);

  const allBalances: InventoryBalance[] = balances ?? [];

  // Filter logic
  const searchQueryLower = searchQuery.toLowerCase().trim();
  let filtered = allBalances.filter(b => {
    if (statusFilter && b.status.key !== statusFilter) {
      return false;
    }
    if (hideZeroStock && b.quantity <= 0) {
      return false;
    }
    if (searchQueryLower) {
      const matchName = b.materialName?.toLowerCase().includes(searchQueryLower);
      const matchCode = b.materialCode?.toLowerCase().includes(searchQueryLower);
      const matchLot = b.lotNumber?.toLowerCase().includes(searchQueryLower);
      const matchSupplier = b.supplierName?.toLowerCase().includes(searchQueryLower);
      const matchRef = b.sourceReference?.toLowerCase().includes(searchQueryLower);
      return matchName || matchCode || matchLot || matchSupplier || matchRef;
    }
    return true;
  });

  // Sorting logic
  filtered = [...filtered].sort((a, b) => {
    switch (sortBy) {
      case 'name-asc':
        return a.materialName.localeCompare(b.materialName, 'pt-BR');
      case 'name-desc':
        return b.materialName.localeCompare(a.materialName, 'pt-BR');
      case 'qty-desc':
        return b.quantity - a.quantity;
      case 'qty-asc':
        return a.quantity - b.quantity;
      case 'date-desc':
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      case 'date-asc':
        return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      default:
        return 0;
    }
  });

  const handleTabChange = (_: React.SyntheticEvent, v: number) => setTab(v);

  const handleClearAllFilters = () => {
    setStatusFilter('');
    setSearchQuery('');
    setHideZeroStock(false);
    setSortBy('name-asc');
  };

  const handleMaterialCreated = (code: string) => {
    setShowCreateModal(false);
    setCreateSuccess(`Material "${code}" cadastrado com sucesso.`);
    void reload();
  };

  // KPI Calculations
  const totalQty = filtered.reduce((sum, b) => sum + b.quantity, 0);
  const blockedCount = filtered.filter(b => b.status.key === 'Blocked').length;

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

      {/* KPI Cards */}
      <Paper variant="outlined" sx={{ mb: 3, bgcolor: 'background.paper', overflow: 'hidden' }}>
        <Grid container>
          <Grid size={{ xs: 12, sm: 4 }} sx={{ borderRight: { sm: '1px solid #edebe9' }, borderBottom: { xs: '1px solid #edebe9', sm: 0 } }}>
            <StatCard
              label="Lotes em Exibição"
              value={loading ? '...' : filtered.length}
              icon={<Package size={20} />}
              color="primary.main"
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }} sx={{ borderRight: { sm: '1px solid #edebe9' }, borderBottom: { xs: '1px solid #edebe9', sm: 0 } }}>
            <StatCard
              label="Quantidade Total"
              value={loading ? '...' : totalQty.toLocaleString('pt-BR')}
              icon={<PackageCheck size={20} />}
              color="success.main"
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }}>
            <StatCard
              label="Lotes Bloqueados"
              value={loading ? '...' : blockedCount}
              icon={<Lock size={20} />}
              color="error.main"
            />
          </Grid>
        </Grid>
      </Paper>

      {/* Search and Filters Bar */}
      <Paper variant="outlined" sx={{ p: 2, mb: 3, bgcolor: '#ffffff' }}>
        <Grid container spacing={2} sx={{ alignItems: 'center' }}>
          {/* Search Field */}
          <Grid size={{ xs: 12, md: 5 }}>
            <TextField
              fullWidth
              size="small"
              variant="outlined"
              placeholder="Pesquisar por material, código, lote ou fornecedor..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon size={18} style={{ color: theme.palette.text.secondary }} />
                    </InputAdornment>
                  ),
                  endAdornment: searchQuery && (
                    <InputAdornment position="end">
                      <IconButton size="small" onClick={() => setSearchQuery('')}>
                        <X size={16} />
                      </IconButton>
                    </InputAdornment>
                  ),
                }
              }}
            />
          </Grid>

          {/* Sort Select */}
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <FormControl fullWidth size="small">
              <InputLabel id="sort-select-label">Ordenar por</InputLabel>
              <Select
                labelId="sort-select-label"
                id="sort-select"
                value={sortBy}
                label="Ordenar por"
                onChange={e => setSortBy(e.target.value)}
              >
                <MenuItem value="name-asc">Nome do Material (A-Z)</MenuItem>
                <MenuItem value="name-desc">Nome do Material (Z-A)</MenuItem>
                <MenuItem value="qty-desc">Quantidade (Maior primeiro)</MenuItem>
                <MenuItem value="qty-asc">Quantidade (Menor primeiro)</MenuItem>
                <MenuItem value="date-desc">Data de Criação (Mais recente)</MenuItem>
                <MenuItem value="date-asc">Data de Criação (Mais antiga)</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Hide Zero Stock Checkbox */}
          <Grid size={{ xs: 12, sm: 6, md: 4 }} sx={{ display: 'flex', alignItems: 'center', justifyContent: { xs: 'flex-start', md: 'flex-end' } }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={hideZeroStock}
                  onChange={e => setHideZeroStock(e.target.checked)}
                  color="primary"
                  size="small"
                />
              }
              label={
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  Ocultar saldo zerado
                </Typography>
              }
            />
          </Grid>
        </Grid>

        {/* Chips and Reset Filters Row */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          sx={{ mt: 2, pt: 2, borderTop: '1px solid', borderColor: 'divider', alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
            <Typography variant="caption" sx={{ alignSelf: 'center', mr: 1, fontWeight: 700, color: 'text.secondary' }}>
              STATUS:
            </Typography>
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
          </Stack>

          <Stack direction="row" spacing={1} sx={{ mt: { xs: 1, sm: 0 }, alignItems: 'center' }}>
            {(searchQuery || statusFilter || hideZeroStock || sortBy !== 'name-asc') && (
              <Button
                size="small"
                variant="text"
                color="secondary"
                onClick={handleClearAllFilters}
                startIcon={<FilterX size={14} />}
                sx={{ fontWeight: 700 }}
              >
                Limpar Filtros
              </Button>
            )}
            <Button
              size="small"
              variant="text"
              startIcon={<RefreshIcon size={14} />}
              onClick={reload}
              disabled={loading}
              sx={{ fontWeight: 700, mr: 1 }}
            >
              Atualizar
            </Button>
            <Divider orientation="vertical" flexItem sx={{ mx: 1, display: { xs: 'none', sm: 'block' } }} />
            <ToggleButtonGroup
              size="small"
              value={viewMode}
              exclusive
              onChange={(_, nextView) => {
                if (nextView !== null) {
                  setViewMode(nextView);
                  localStorage.setItem('inventory_view_mode', nextView);
                }
              }}
              aria-label="modo de exibição"
              sx={{ height: 32 }}
            >
              <ToggleButton value="grid" aria-label="grade de cards" sx={{ px: 1.5 }}>
                <LayoutGrid size={14} />
              </ToggleButton>
              <ToggleButton value="table" aria-label="tabela" sx={{ px: 1.5 }}>
                <List size={14} />
              </ToggleButton>
            </ToggleButtonGroup>
          </Stack>
        </Stack>
      </Paper>

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
          <Typography variant="body2" color="text.disabled" sx={{ mb: 2 }}>
            {searchQuery || statusFilter || hideZeroStock
              ? 'Tente ajustar os termos de pesquisa ou remover os filtros aplicados.'
              : tab === 0
              ? 'Materiais entram via importação de nota fiscal (supply chain).'
              : 'Produtos acabados aparecem aqui após conclusão de ordens de produção.'}
          </Typography>
          {(searchQuery || statusFilter || hideZeroStock) && (
            <Button variant="outlined" size="small" onClick={handleClearAllFilters}>
              Limpar Filtros
            </Button>
          )}
        </Paper>
      ) : viewMode === 'grid' ? (
        <InventoryBalanceCardList
          balances={filtered}
          onNavigate={code => navigate(`/app/inventory/materials/${code}`)}
          onDetails={id => setSelectedBalanceId(id)}
        />
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

