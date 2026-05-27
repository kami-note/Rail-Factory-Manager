import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  InputAdornment,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { BookOpen, Plus, Search } from 'lucide-react';
import { ModuleHeader } from '../../../../shared/components/common/ModuleHeader';
import { InlineError } from '../../../../shared/components/common/InlineError';
import { Authorized } from '../../../auth';
import { activateBom } from '../../api/production';
import { useBoms } from '../../hooks/useBoms';
import { MaterialCodeAutocomplete } from '../../../inventory';
import type { Bom } from '../../types';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import { CreateBomModal } from './CreateBomModal';
import { ProductGroup } from './ProductGroup';

type BomsPageProps = {
  tenantCode: string;
};

export function BomsPage({ tenantCode }: BomsPageProps) {
  const [filter, setFilter] = useState('');
  const { data: fetchedBoms, loading, error: fetchError } = useBoms(tenantCode);
  const [allBoms, setAllBoms] = useState<Bom[]>([]);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [activatingId, setActivatingId] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const error = fetchError ?? mutationError;

  useEffect(() => {
    if (fetchedBoms) setAllBoms(fetchedBoms);
  }, [fetchedBoms]);

  const filtered = filter.trim()
    ? allBoms.filter(b => b.productCode.includes(filter.trim().toUpperCase()))
    : allBoms;

  const grouped = filtered.reduce<Record<string, Bom[]>>((acc, bom) => {
    (acc[bom.productCode] ??= []).push(bom);
    return acc;
  }, {});

  const handleBomCreated = (newBom: Bom) => {
    setAllBoms(prev => [newBom, ...prev]);
    setExpandedId(newBom.id);
    setSuccess(`BOM v${newBom.version} criada para ${newBom.productCode}.`);
    setShowCreateModal(false);
  };

  const handleActivate = async (bomId: string) => {
    setActivatingId(bomId);
    setMutationError(null);
    try {
      await activateBom(tenantCode, bomId);
      setAllBoms(prev =>
        prev.map(b =>
          b.id === bomId ? { ...b, status: { key: 'Active', label: 'Ativo', color: 'success' } } : b
        )
      );
      setSuccess('BOM ativada com sucesso.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Não foi possível ativar a BOM.'));
    } finally {
      setActivatingId(null);
    }
  };

  const handleItemAdded = (bomId: string, updatedBom: Bom) => {
    setAllBoms(prev => prev.map(b => (b.id === bomId ? updatedBom : b)));
  };

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="ESTRUTURAS DE PRODUTO (BOM)"
        icon={<BookOpen size={20} />}
        action={
          <Authorized permission="production.write">
            <Button
              variant="contained"
              size="small"
              startIcon={<Plus size={16} />}
              onClick={() => setShowCreateModal(true)}
            >
              Nova BOM
            </Button>
          </Authorized>
        }
      />

      {success && (
        <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}
      {error && <InlineError message={error} marginBottom={2} />}

      {/* Filter bar */}
      <Paper variant="outlined" sx={{ p: 2, mt: 3 }}>
        <MaterialCodeAutocomplete
          tenantCode={tenantCode}
          value={filter}
          onInputChange={setFilter}
          onMaterialSelect={m => setFilter(m.materialCode)}
          placeholder="Filtrar por código do produto..."
          fullWidth
          startAdornment={<InputAdornment position="start"><Search size={16} /></InputAdornment>}
          category="FinishedGood"
        />
      </Paper>

      {/* List */}
      <Box sx={{ mt: 3 }}>
        {loading ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <CircularProgress size={32} />
          </Box>
        ) : Object.keys(grouped).length === 0 ? (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
            {filter ? `Nenhuma BOM encontrada para "${filter}".` : 'Nenhuma BOM cadastrada.'}
          </Typography>
        ) : (
          <Stack spacing={1}>
            {Object.entries(grouped).map(([productCode, boms]) => (
              <ProductGroup
                key={productCode}
                productCode={productCode}
                boms={boms}
                tenantCode={tenantCode}
                expandedId={expandedId}
                activatingId={activatingId}
                onToggle={id => setExpandedId(expandedId === id ? null : id)}
                onActivate={id => void handleActivate(id)}
                onItemAdded={handleItemAdded}
              />
            ))}
          </Stack>
        )}
      </Box>

      {/* Create BOM Modal */}
      <CreateBomModal
        open={showCreateModal}
        tenantCode={tenantCode}
        onCreated={handleBomCreated}
        onClose={() => setShowCreateModal(false)}
      />
    </Box>
  );
}
