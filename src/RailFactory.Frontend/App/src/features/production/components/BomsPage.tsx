import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Divider,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography
} from '@mui/material';
import { BookOpen, ChevronDown, ChevronRight, Plus, Search, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { InlineError } from '../../../shared/components/common/InlineError';
import { Authorized } from '../../auth';
import { listBoms, createBom, addBomItem, activateBom, getBom } from '../api/production';
import { searchMaterials } from '../../inventory';
import type { Bom } from '../types';
import type { MaterialSearchResult } from '../../inventory';
import { toUiErrorMessage } from '../../../shared/lib/http';

type BomsPageProps = {
  tenantCode: string;
};

export function BomsPage({ tenantCode }: BomsPageProps) {
  const [filter, setFilter] = useState('');
  const [allBoms, setAllBoms] = useState<Bom[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [activatingId, setActivatingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setAllBoms(await listBoms(tenantCode));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar as BOMs.'));
    } finally {
      setLoading(false);
    }
  }, [tenantCode]);

  useEffect(() => { void loadAll(); }, [loadAll]);

  const filtered = filter.trim()
    ? allBoms.filter(b => b.productCode.includes(filter.trim().toUpperCase()))
    : allBoms;

  // Group by product code
  const grouped = filtered.reduce<Record<string, Bom[]>>((acc, bom) => {
    (acc[bom.productCode] ??= []).push(bom);
    return acc;
  }, {});

  const handleCreateBom = async () => {
    if (!filter.trim()) return;
    setCreating(true);
    setError(null);
    try {
      const newBom = await createBom(tenantCode, { productCode: filter.trim().toUpperCase() });
      setAllBoms(prev => [newBom, ...prev]);
      setExpandedId(newBom.id);
      setSuccess(`BOM v${newBom.version} criada para ${newBom.productCode}.`);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar a BOM.'));
    } finally {
      setCreating(false);
    }
  };

  const handleActivate = async (bomId: string) => {
    setActivatingId(bomId);
    setError(null);
    try {
      await activateBom(tenantCode, bomId);
      setAllBoms(prev => prev.map(b => b.id === bomId ? { ...b, status: 'Active' } : b));
      setSuccess('BOM ativada com sucesso.');
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível ativar a BOM.'));
    } finally {
      setActivatingId(null);
    }
  };

  const handleItemAdded = (bomId: string, updatedBom: Bom) => {
    setAllBoms(prev => prev.map(b => b.id === bomId ? updatedBom : b));
  };

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="ESTRUTURAS DE PRODUTO (BOM)"
        icon={<BookOpen size={20} />}
      />

      {success && <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>{success}</Alert>}
      {error && <InlineError message={error} marginBottom={2} />}

      <Paper variant="outlined" sx={{ p: 2, mt: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Filtrar por código do produto..."
            value={filter}
            onChange={e => setFilter(e.target.value.toUpperCase())}
            sx={{ flexGrow: 1 }}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start"><Search size={16} /></InputAdornment>,
                style: { fontFamily: 'monospace', fontWeight: 700 }
              }
            }}
          />
          <Authorized permission="production.write">
            <Button
              variant="outlined"
              startIcon={creating ? <CircularProgress size={16} color="inherit" /> : <Plus size={16} />}
              onClick={() => void handleCreateBom()}
              disabled={creating || !filter.trim()}
              title="Cria nova BOM para o código filtrado"
            >
              Nova BOM
            </Button>
          </Authorized>
        </Stack>
      </Paper>

      <Box sx={{ mt: 3 }}>
        {loading ? (
          <Box sx={{ textAlign: 'center', py: 6 }}><CircularProgress size={32} /></Box>
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
    </Box>
  );
}

function ProductGroup({ productCode, boms, tenantCode, expandedId, activatingId, onToggle, onActivate, onItemAdded }: {
  productCode: string;
  boms: Bom[];
  tenantCode: string;
  expandedId: string | null;
  activatingId: string | null;
  onToggle: (id: string) => void;
  onActivate: (id: string) => void;
  onItemAdded: (bomId: string, updated: Bom) => void;
}) {
  const activeCount = boms.filter(b => b.status === 'Active').length;

  return (
    <Paper variant="outlined">
      <Box sx={{ px: 2, py: 1.5, bgcolor: 'grey.50', borderBottom: '1px solid', borderColor: 'divider' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 800, fontFamily: 'monospace', flexGrow: 1 }}>
            {productCode}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {boms.length} {boms.length === 1 ? 'versão' : 'versões'}
          </Typography>
          {activeCount > 0 && <Chip size="small" label="tem versão ativa" color="success" variant="outlined" />}
        </Stack>
      </Box>

      <Stack>
        {boms.map((bom, idx) => (
          <Box key={bom.id} sx={{ borderTop: idx > 0 ? '1px solid' : 'none', borderColor: 'divider' }}>
            <BomCard
              bom={bom}
              tenantCode={tenantCode}
              expanded={expandedId === bom.id}
              activating={activatingId === bom.id}
              onToggle={() => onToggle(bom.id)}
              onActivate={() => onActivate(bom.id)}
              onItemAdded={updated => onItemAdded(bom.id, updated)}
            />
          </Box>
        ))}
      </Stack>
    </Paper>
  );
}

function BomCard({ bom, tenantCode, expanded, activating, onToggle, onActivate, onItemAdded }: {
  bom: Bom;
  tenantCode: string;
  expanded: boolean;
  activating: boolean;
  onToggle: () => void;
  onActivate: () => void;
  onItemAdded: (updated: Bom) => void;
}) {
  const [showAddItem, setShowAddItem] = useState(false);

  return (
    <>
      <Stack
        direction="row"
        sx={{ px: 2, py: 1.5, alignItems: 'center', cursor: 'pointer', userSelect: 'none', '&:hover': { bgcolor: 'action.hover' } }}
        onClick={onToggle}
      >
        <IconButton size="small" sx={{ mr: 1 }}>
          {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        </IconButton>
        <Stack direction="row" spacing={2} sx={{ flexGrow: 1, alignItems: 'center' }}>
          <Chip size="small" label={`v${bom.version}`} variant="outlined" />
          <Chip
            size="small"
            label={bom.status === 'Active' ? 'Ativa' : 'Rascunho'}
            color={bom.status === 'Active' ? 'success' : 'default'}
            variant={bom.status === 'Active' ? 'filled' : 'outlined'}
          />
          <Typography variant="caption" color="text.secondary">
            {bom.items.length} {bom.items.length === 1 ? 'componente' : 'componentes'}
          </Typography>
        </Stack>
        {bom.status === 'Draft' && (
          <Authorized permission="production.write">
            <Tooltip title="Ativar esta versão">
              <Button
                size="small"
                variant="contained"
                color="success"
                startIcon={activating ? <CircularProgress size={14} color="inherit" /> : <Zap size={14} />}
                onClick={e => { e.stopPropagation(); onActivate(); }}
                disabled={activating}
                sx={{ fontWeight: 800 }}
              >
                Ativar
              </Button>
            </Tooltip>
          </Authorized>
        )}
      </Stack>

      <Collapse in={expanded}>
        <Divider />
        <Box sx={{ p: 2 }}>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>UNIDADE</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bom.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={3} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                      Sem componentes. Adicione itens abaixo.
                    </TableCell>
                  </TableRow>
                ) : bom.items.map(item => (
                  <TableRow key={item.id}>
                    <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{item.materialCode}</TableCell>
                    <TableCell align="right">{item.quantity}</TableCell>
                    <TableCell>{item.unitOfMeasure}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          {bom.status === 'Draft' && (
            <Authorized permission="production.write">
              <Box sx={{ mt: 2 }}>
                {showAddItem ? (
                  <AddBomItemForm
                    tenantCode={tenantCode}
                    bom={bom}
                    onAdded={updated => { onItemAdded(updated); setShowAddItem(false); }}
                    onCancel={() => setShowAddItem(false)}
                  />
                ) : (
                  <Button size="small" startIcon={<Plus size={14} />} onClick={() => setShowAddItem(true)}>
                    Adicionar Componente
                  </Button>
                )}
              </Box>
            </Authorized>
          )}
        </Box>
      </Collapse>
    </>
  );
}

function AddBomItemForm({ tenantCode, bom, onAdded, onCancel }: {
  tenantCode: string;
  bom: Bom;
  onAdded: (updated: Bom) => void;
  onCancel: () => void;
}) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [unitOfMeasure, setUnitOfMeasure] = useState('UN');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [options, setOptions] = useState<MaterialSearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  const handleMaterialInputChange = (_: React.SyntheticEvent, value: string) => {
    const upper = value.toUpperCase();
    setMaterialCode(upper);

    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (upper.length < 2) { setOptions([]); return; }

    debounceRef.current = setTimeout(async () => {
      setSearching(true);
      try {
        setOptions(await searchMaterials(tenantCode, upper));
      } catch {
        setOptions([]);
      } finally {
        setSearching(false);
      }
    }, 300);
  };

  const handleSubmit = async () => {
    if (!materialCode.trim() || !quantity || !unitOfMeasure.trim()) return;
    setSaving(true);
    setError(null);
    try {
      await addBomItem(tenantCode, bom.id, {
        materialCode: materialCode.trim().toUpperCase(),
        quantity: Number(quantity),
        unitOfMeasure: unitOfMeasure.trim().toUpperCase()
      });
      // Reload the real BOM from the server to get the actual item ID assigned by the backend
      const freshBom = await getBom(tenantCode, bom.id);
      onAdded(freshBom);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível adicionar o componente.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box sx={{ mt: 1 }}>
      {error && <InlineError message={error} marginBottom={1} />}
      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
        <Autocomplete
          freeSolo
          sx={{ flexGrow: 1 }}
          options={options}
          getOptionLabel={o => typeof o === 'string' ? o : o.materialCode}
          filterOptions={x => x}
          loading={searching}
          inputValue={materialCode}
          onInputChange={handleMaterialInputChange}
          onChange={(_, value) => {
            if (value && typeof value !== 'string') {
              setMaterialCode(value.materialCode);
              if (value.stockUnit) setUnitOfMeasure(value.stockUnit.toUpperCase());
            }
          }}
          renderOption={(props, option) => (
            <Box component="li" {...props} key={option.materialCode}>
              <Stack>
                <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>
                  {option.materialCode}
                </Typography>
                <Typography variant="caption" color="text.secondary" noWrap>
                  {option.officialName}
                </Typography>
              </Stack>
            </Box>
          )}
          renderInput={params => (
            <TextField
              {...params}
              label="Código do material"
              size="small"
              slotProps={{
                input: {
                  ...params.slotProps?.input,
                  endAdornment: (
                    <>
                      {searching && <CircularProgress size={14} />}
                      {params.slotProps?.input && typeof params.slotProps.input === 'object' && 'endAdornment' in params.slotProps.input
                        ? (params.slotProps.input as { endAdornment?: React.ReactNode }).endAdornment
                        : null}
                    </>
                  ),
                  style: { fontFamily: 'monospace', fontWeight: 700 }
                }
              }}
            />
          )}
        />
        <TextField
          label="Qtd"
          type="number"
          size="small"
          sx={{ width: 90 }}
          value={quantity}
          onChange={e => setQuantity(e.target.value)}
        />
        <TextField
          label="Unidade"
          size="small"
          sx={{ width: 90 }}
          value={unitOfMeasure}
          onChange={e => setUnitOfMeasure(e.target.value.toUpperCase())}
        />
        <Button
          variant="contained"
          size="small"
          onClick={() => void handleSubmit()}
          disabled={saving || !materialCode.trim()}
          sx={{ fontWeight: 800, alignSelf: 'center' }}
        >
          {saving ? <CircularProgress size={16} color="inherit" /> : 'Salvar'}
        </Button>
        <Button size="small" onClick={onCancel} disabled={saving} sx={{ alignSelf: 'center' }}>
          Cancelar
        </Button>
      </Stack>
    </Box>
  );
}
