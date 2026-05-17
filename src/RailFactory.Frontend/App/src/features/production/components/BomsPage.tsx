import React, { useState } from 'react';
import {
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
import { listBoms, createBom, addBomItem, activateBom } from '../api/production';
import type { Bom } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type BomsPageProps = {
  tenantCode: string;
};

export function BomsPage({ tenantCode }: BomsPageProps) {
  const [productCode, setProductCode] = useState('');
  const [boms, setBoms] = useState<Bom[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const handleSearch = async () => {
    if (!productCode.trim()) return;
    setLoading(true);
    setError(null);
    setSearched(true);
    try {
      setBoms(await listBoms(tenantCode, productCode.trim()));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar as BOMs.'));
    } finally {
      setLoading(false);
    }
  };

  const handleCreateBom = async () => {
    if (!productCode.trim()) return;
    setError(null);
    try {
      const newBom = await createBom(tenantCode, { productCode: productCode.trim().toUpperCase() });
      setBoms(prev => [newBom, ...prev]);
      setExpandedId(newBom.id);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar a BOM.'));
    }
  };

  const handleActivate = async (bomId: string) => {
    setError(null);
    try {
      await activateBom(tenantCode, bomId);
      setBoms(prev => prev.map(b => b.id === bomId ? { ...b, status: 'Active' } : b));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível ativar a BOM.'));
    }
  };

  const handleItemAdded = (bomId: string, updatedBom: Bom) => {
    setBoms(prev => prev.map(b => b.id === bomId ? updatedBom : b));
  };

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="ESTRUTURAS DE PRODUTO (BOM)"
        icon={<BookOpen size={20} />}
      />

      {error && <InlineError message={error} marginBottom={2} />}

      <Paper variant="outlined" sx={{ p: 2, mt: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Código do produto (ex: TRILHO-60)"
            value={productCode}
            onChange={e => setProductCode(e.target.value.toUpperCase())}
            onKeyDown={e => e.key === 'Enter' && void handleSearch()}
            sx={{ flexGrow: 1 }}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start"><Search size={16} /></InputAdornment>,
                style: { fontFamily: 'monospace', fontWeight: 700 }
              }
            }}
          />
          <Button
            variant="contained"
            onClick={() => void handleSearch()}
            disabled={loading || !productCode.trim()}
            startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <Search size={16} />}
          >
            Buscar
          </Button>
          <Authorized permission="production.write">
            <Button
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => void handleCreateBom()}
              disabled={!productCode.trim()}
            >
              Nova BOM
            </Button>
          </Authorized>
        </Stack>
      </Paper>

      {searched && (
        <Box sx={{ mt: 3 }}>
          {boms.length === 0 ? (
            <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
              Nenhuma BOM encontrada para <strong>{productCode}</strong>.
            </Typography>
          ) : (
            <Stack spacing={2}>
              {boms.map(bom => (
                <BomCard
                  key={bom.id}
                  bom={bom}
                  tenantCode={tenantCode}
                  expanded={expandedId === bom.id}
                  onToggle={() => setExpandedId(expandedId === bom.id ? null : bom.id)}
                  onActivate={() => void handleActivate(bom.id)}
                  onItemAdded={(updated) => handleItemAdded(bom.id, updated)}
                />
              ))}
            </Stack>
          )}
        </Box>
      )}
    </Box>
  );
}

function BomCard({ bom, tenantCode, expanded, onToggle, onActivate, onItemAdded }: {
  bom: Bom;
  tenantCode: string;
  expanded: boolean;
  onToggle: () => void;
  onActivate: () => void;
  onItemAdded: (updated: Bom) => void;
}) {
  const [showAddItem, setShowAddItem] = useState(false);

  return (
    <Paper variant="outlined">
      <Stack
        direction="row"
        sx={{ p: 2, alignItems: 'center', cursor: 'pointer', userSelect: 'none' }}
        onClick={onToggle}
      >
        <IconButton size="small" sx={{ mr: 1 }}>
          {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, fontFamily: 'monospace' }}>
              {bom.productCode}
            </Typography>
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
        </Box>
        {bom.status === 'Draft' && (
          <Authorized permission="production.write">
            <Tooltip title="Ativar esta versão">
              <Button
                size="small"
                variant="contained"
                color="success"
                startIcon={<Zap size={14} />}
                onClick={e => { e.stopPropagation(); onActivate(); }}
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
    </Paper>
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
      const updatedBom: Bom = {
        ...bom,
        items: [
          ...bom.items,
          {
            id: crypto.randomUUID(),
            materialCode: materialCode.trim().toUpperCase(),
            quantity: Number(quantity),
            unitOfMeasure: unitOfMeasure.trim().toUpperCase()
          }
        ]
      };
      onAdded(updatedBom);
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
        <TextField
          label="Código do material"
          size="small"
          value={materialCode}
          onChange={e => setMaterialCode(e.target.value.toUpperCase())}
          sx={{ flexGrow: 1 }}
          slotProps={{ htmlInput: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
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
