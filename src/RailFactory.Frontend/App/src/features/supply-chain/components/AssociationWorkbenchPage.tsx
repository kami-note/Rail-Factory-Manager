import React, { useEffect, useState, useMemo } from 'react';
import {
  Box,
  Typography,
  CircularProgress,
  Alert,
  Paper,
  Divider,
  List,
  ListItemButton,
  ListItemText,
  Chip,
  Stack,
  Button,
  TextField,
  Autocomplete,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  useTheme,
  alpha,
  IconButton,
  Tooltip,
  MenuItem
} from '@mui/material';
import {
  RefreshCw as RefreshIcon,
  Plus as AddIcon,
  Save as SaveIcon,
  Clock as ReviewLaterIcon,
  Trash2 as IgnoreIcon,
  ArrowRight as ReleaseIcon,
  HelpCircle as HelpIcon,
  Link2 as AssociationIcon,
  Edit2 as EditIcon
} from 'lucide-react';
import { useLocation } from 'react-router-dom';
import { searchMaterials, MaterialSearchResult } from '../../inventory';
import { 
  getAssociationQueue, 
  getAssociationWorkbench, 
  associateReceiptItem, 
  createMaterialAndAssociate, 
  recordControlledDecision, 
  releaseToConference,
  overrideSupplierProductCode
} from '../api/workbench';
import { 
  AssociationQueueItem, 
  AssociationWorkbench as WorkbenchData, 
  WorkbenchItem
} from '../types';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { Authorized } from '../../auth';

// Use lucide icons consistently with the layout
const RefreshIconLucide = () => <RefreshIcon size={16} />;
const ReleaseIconLucide = () => <ReleaseIcon size={16} />;

interface AssociationWorkbenchPageProps {
  tenantCode: string;
}

/**
 * Full-screen operational workbench for resolving supplier SKU -> internal material SKU decisions.
 * Replaces the legacy modal-based flow (AssociationInbox/Forge).
 * 
 * @remarks
 * Security: Mutation endpoints are protected by CSRF tokens handled automatically by the http client.
 * Concurrency: Uses aggregate-level versioning via receipt.version (UpdatedAt) for atomic releases.
 */
export function AssociationWorkbenchPage({ tenantCode }: AssociationWorkbenchPageProps) {
  const theme = useTheme();
  const location = useLocation();
  const [queue, setQueue] = useState<AssociationQueueItem[]>([]);
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);
  const [workbench, setWorkbench] = useState<WorkbenchData | null>(null);
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);
  const [loadingQueue, setLoadingQueue] = useState(true);
  const [loadingWorkbench, setLoadingWorkbench] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedItem = useMemo(() => 
    workbench?.items.find(i => i.itemId === selectedItemId), 
    [workbench, selectedItemId]);

  const loadQueue = async () => {
    setLoadingQueue(true);
    try {
      const data = await getAssociationQueue(tenantCode);
      setQueue(data);
      
      // Support deep linking from ReceiptsList
      const params = new URLSearchParams(location.search);
      const targetReceiptId = params.get('receiptId');
      
      if (targetReceiptId) {
        setSelectedReceiptId(targetReceiptId);
      } else if (data.length > 0 && !selectedReceiptId) {
        setSelectedReceiptId(data[0].receiptId);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao carregar fila de associação.');
    } finally {
      setLoadingQueue(false);
    }
  };

  const loadWorkbench = async (receiptId: string) => {
    setLoadingWorkbench(true);
    try {
      const data = await getAssociationWorkbench(tenantCode, receiptId);
      setWorkbench(data);
      if (data.items.length > 0) {
        const firstPending = data.items.find(i => i.associationStatus === 'Pending' || i.associationStatus === 'Conflict');
        setSelectedItemId(firstPending?.itemId ?? data.items[0].itemId);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao carregar detalhes da bancada.');
    } finally {
      setLoadingWorkbench(false);
    }
  };

  useEffect(() => {
    void loadQueue();
  }, [tenantCode, location.search]);

  useEffect(() => {
    if (selectedReceiptId) {
      void loadWorkbench(selectedReceiptId);
    }
  }, [selectedReceiptId]);

  const handleSelectReceipt = (receiptId: string) => {
    setSelectedReceiptId(receiptId);
    setWorkbench(null);
    setSelectedItemId(null);
  };

  const handleDecisionSuccess = (updatedItem: any) => {
    if (!workbench) return;

    // 1. Update the local items state
    const newItems: WorkbenchItem[] = workbench.items.map(i => 
      i.itemId === updatedItem.itemId ? { ...i, ...updatedItem } : i
    );
    
    // 2. Recalculate release status locally to provide immediate feedback
    const unresolvedCount = newItems.filter(x => 
      x.associationStatus !== 'Mapped' && x.associationStatus !== 'CreatedAndMapped'
    ).length;

    const canRelease = unresolvedCount === 0;
    const releaseBlockers = unresolvedCount > 0 
      ? [`${unresolvedCount} item(s) exigem decisões de associação.`] 
      : [];

    // 3. Simple check to advance to next item
    const nextItem = newItems.find((i, idx) => 
      idx > newItems.findIndex(x => x.itemId === updatedItem.itemId) && 
      (i.associationStatus === 'Pending' || i.associationStatus === 'Conflict' || i.associationStatus === 'ReviewLater')
    );

    // 4. Update the entire workbench state
    setWorkbench({ 
      ...workbench, 
      receipt: {
        ...workbench.receipt,
        canReleaseToConference: canRelease,
        releaseBlockers: releaseBlockers
      },
      items: newItems 
    });

    if (nextItem) {
      setSelectedItemId(nextItem.itemId);
    }

    // Refresh queue in background for the sidebar
    void getAssociationQueue(tenantCode).then(setQueue);
  };

  return (
    <Box sx={{ height: 'calc(100vh - 140px)', display: 'flex', flexDirection: 'column' }}>
      <ModuleHeader 
        label="BANCADA DE ASSOCIAÇÃO" 
        icon={<AssociationIcon size={20} />}
      />

      {/* Layout Principal: Fila | Grid | Decisão */}
      <Box sx={{ flexGrow: 1, overflow: 'hidden', display: 'flex', flexDirection: { xs: 'column', md: 'row' } }}>
        
        {/* Sidebar: Fila de Recebimentos */}
        <Box sx={{ width: { xs: '100%', md: '25%' }, borderRight: 1, borderColor: 'divider', display: 'flex', flexDirection: 'column', bgcolor: 'background.paper' }}>
          <Box sx={{ p: 2, bgcolor: alpha(theme.palette.primary.main, 0.05) }}>
            <Typography variant="overline" sx={{ fontWeight: 800 }}>NOTAS PENDENTES ({queue.length})</Typography>
          </Box>
          <Divider />
          <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
            {loadingQueue ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress size={24} /></Box>
            ) : (
              <List sx={{ p: 0 }}>
                {queue.map((item) => (
                  <ListItemButton 
                    key={item.receiptId}
                    selected={selectedReceiptId === item.receiptId}
                    onClick={() => handleSelectReceipt(item.receiptId)}
                    sx={{ 
                      borderBottom: 1, 
                      borderColor: 'divider',
                      py: 2,
                      '&.Mui-selected': {
                        borderLeft: 4,
                        borderColor: 'primary.main',
                      }
                    }}
                  >
                    <ListItemText 
                      primary={
                        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{item.receiptNumber}</Typography>
                          <Chip 
                            size="small" 
                            label={`${item.resolvedItems}/${item.totalItems}`} 
                            color={item.blockingItems === 0 ? "success" : "warning"}
                            variant={selectedReceiptId === item.receiptId ? "filled" : "outlined"}
                          />
                        </Stack>
                      }
                      secondary={
                        <Box component="span" sx={{ display: 'block', mt: 0.5 }}>
                          <Typography variant="caption" color="text.secondary" component="span" sx={{ display: 'block' }}>{item.supplierName}</Typography>
                          <Typography variant="caption" color="text.disabled" component="span">Doc: {item.documentNumber}</Typography>
                        </Box>
                      }
                    />
                  </ListItemButton>
                ))}
              </List>
            )}
          </Box>
          <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider' }}>
            <Button fullWidth startIcon={<RefreshIconLucide />} onClick={loadQueue} size="small">
              Atualizar Fila
            </Button>
          </Box>
        </Box>

        {/* Centro: Grid de Itens da NF-e */}
        <Box sx={{ flexGrow: 1, width: { xs: '100%', md: '50%' }, display: 'flex', flexDirection: 'column', bgcolor: 'grey.50' }}>
          {loadingWorkbench ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}><CircularProgress /></Box>
          ) : workbench ? (
            <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box sx={{ p: 2, bgcolor: 'background.paper', borderBottom: 1, borderColor: 'divider' }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 800 }}>{workbench.receipt.receiptNumber}</Typography>
                    <Typography variant="caption" color="text.secondary">{workbench.receipt.supplierName} • {workbench.items.length} itens</Typography>
                  </Box>
                  <Authorized permission="supplychain.write">
                    <Button 
                      variant="contained" 
                      color="primary" 
                      disabled={!workbench.receipt.canReleaseToConference}
                      startIcon={<ReleaseIconLucide />}
                      onClick={async () => {
                        try {
                          await releaseToConference(tenantCode, workbench.receipt.id, { expectedVersion: workbench.receipt.version }); 
                          void loadQueue();
                          setWorkbench(null);
                          setSelectedReceiptId(null);
                        } catch (err) {
                          setError(err instanceof Error ? err.message : 'Falha na liberação');
                        }
                      }}
                    >
                      Liberar para Conferência
                    </Button>
                  </Authorized>
                </Stack>
              </Box>

              <TableContainer sx={{ flexGrow: 1, overflowY: 'auto' }}>
                <Table stickyHeader size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>SKU (NF-e)</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>DESCRIÇÃO</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>QTD</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>MATERIAL INTERNO</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {workbench.items.map((item) => {
                      const isSelected = selectedItemId === item.itemId;
                      return (
                        <TableRow 
                          key={item.itemId} 
                          hover 
                          selected={isSelected}
                          onClick={() => setSelectedItemId(item.itemId)}
                          sx={{ cursor: 'pointer' }}
                        >
                          <TableCell>
                            <StatusChip status={item.associationStatus} />
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontWeight: 600, fontFamily: 'monospace' }}>{item.supplierProductCode}</Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2" noWrap sx={{ maxWidth: 250 }}>{item.description}</Typography>
                            <Typography variant="caption" color="text.disabled">{item.ncm ? `NCM: ${item.ncm}` : ''}</Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Typography variant="body2">{item.quantity} {item.supplierUnit}</Typography>
                          </TableCell>
                          <TableCell>
                            {item.internalMaterialCode ? (
                              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                                <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main' }}>{item.internalMaterialCode}</Typography>
                                {item.conversionFactor && item.conversionFactor !== 1 && (
                                  <Typography variant="caption" color="text.secondary">x{item.conversionFactor}</Typography>
                                )}
                              </Stack>
                            ) : (
                              <Typography variant="caption" color="text.disabled">Pendente</Typography>
                            )}
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          ) : (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%', p: 4, textAlign: 'center' }}>
              <Box>
                <Box sx={{ color: 'text.disabled', mb: 2 }}><HelpIcon size={64} /></Box>
                <Typography variant="h5" color="text.secondary">Selecione uma nota para iniciar a associação</Typography>
              </Box>
            </Box>
          )}
        </Box>

        {/* Side Panel: Painel de Decisão */}
        <Box sx={{ width: { xs: '100%', md: '25%' }, borderLeft: 1, borderColor: 'divider', bgcolor: 'background.paper', overflowY: 'auto' }}>
          {selectedItem ? (
            <DecisionPanel 
              tenantCode={tenantCode}
              receiptId={selectedReceiptId!}
              item={selectedItem}
              onSuccess={handleDecisionSuccess}
            />
          ) : (
            <Box sx={{ p: 4, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">Selecione um item no grid para resolver a associação.</Typography>
            </Box>
          )}
        </Box>
      </Box>

      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ position: 'fixed', bottom: 24, left: 24, right: 24, zIndex: 2000 }}>
          {error}
        </Alert>
      )}
    </Box>
  );
}

function DecisionPanel({ tenantCode, receiptId, item, onSuccess }: { 
  tenantCode: string, 
  receiptId: string, 
  item: WorkbenchItem, 
  onSuccess: (updated: any) => void 
}) {
  const theme = useTheme();
  const [tab, setTab] = useState<'match' | 'create'>('match');
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<MaterialSearchResult[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [selectedMaterial, setSelectedMaterial] = useState<MaterialSearchResult | null>(null);
  const [conversionFactor, setConversionFactor] = useState<number>(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSearch = async (query: string) => {
    setSearchLoading(true);
    try {
      const results = await searchMaterials(tenantCode, query);
      setSearchResults(results);
    } catch (err) {
      console.error('Search failed', err);
    } finally {
      setSearchLoading(false);
    }
  };

  useEffect(() => {
    if (searchQuery.length >= 2) {
      const h = setTimeout(() => void handleSearch(searchQuery), 350);
      return () => clearTimeout(h);
    }
  }, [searchQuery]);

  const handleMapExisting = async (material: MaterialSearchResult | { materialCode: string, officialName: string, stockUnit?: string }) => {
    setIsSubmitting(true);
    setError(null);
    try {
      const result = await associateReceiptItem(tenantCode, receiptId, item.itemId, {
        expectedVersion: item.version,
        internalMaterialCode: material.materialCode,
        conversionFactor: conversionFactor
      });
      onSuccess(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na associação');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOverrideSku = async () => {
    const corrected = window.prompt("SKU do Fornecedor Corrigido:", item.supplierProductCode);
    if (!corrected || corrected === item.supplierProductCode) return;
    
    const reason = window.prompt("Motivo da correção (obrigatório):");
    if (!reason) return;

    setIsSubmitting(true);
    try {
      const res = await overrideSupplierProductCode(tenantCode, receiptId, item.itemId, {
        expectedVersion: item.version,
        correctedCode: corrected,
        reason: reason
      });
      onSuccess(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na correção de SKU');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>DADOS FISCAIS DO ITEM</Typography>
      <Box sx={{ mt: 1, mb: 3 }}>
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.2 }}>{item.description}</Typography>
          <Authorized permission="supplychain.write">
            <Tooltip title="Corrigir SKU do Fornecedor">
              <IconButton size="small" onClick={handleOverrideSku} disabled={isSubmitting}>
                <EditIcon size={14} />
              </IconButton>
            </Tooltip>
          </Authorized>
        </Stack>
        <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
          <Chip size="small" label={`Cod: ${item.supplierProductCode}`} variant="outlined" />
          <Chip size="small" label={`${item.quantity} ${item.supplierUnit}`} variant="outlined" />
        </Stack>
      </Box>

      <Divider sx={{ mb: 3 }} />

      <Authorized 
        permission="supplychain.write"
        fallback={
          <Alert severity="info" variant="outlined" sx={{ fontWeight: 600 }}>
            Você possui acesso apenas para visualização deste item.
          </Alert>
        }
      >
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Button 
            fullWidth 
            variant={tab === 'match' ? 'contained' : 'outlined'} 
            onClick={() => setTab('match')}
            size="small"
          >
            Vincular Existente
          </Button>
          <Button 
            fullWidth 
            variant={tab === 'create' ? 'contained' : 'outlined'} 
            onClick={() => setTab('create')}
            size="small"
          >
            Criar Novo
          </Button>
        </Stack>

        {tab === 'match' && (
          <Stack spacing={3}>
            {/* Sugestões do Sistema */}
            {item.suggestions.length > 0 && (
              <Box>
                <Typography variant="caption" color="primary.main" sx={{ fontWeight: 800 }}>SUGESTÕES DO SISTEMA</Typography>
                <List sx={{ mt: 1, p: 0 }}>
                  {item.suggestions.map(s => (
                    <ListItemButton 
                      key={s.materialCode} 
                      onClick={() => {
                        setSelectedMaterial({ 
                          materialCode: s.materialCode, 
                          officialName: s.officialName, 
                          description: '', 
                          category: '',
                          stockUnit: s.stockUnit
                        });
                      }}
                      sx={{ 
                        px: 1.5, 
                        py: 1, 
                        border: 1, 
                        borderColor: 'divider', 
                        borderRadius: 1, 
                        mb: 1,
                        bgcolor: selectedMaterial?.materialCode === s.materialCode ? alpha(theme.palette.primary.main, 0.05) : 'transparent'
                      }}
                    >
                      <ListItemText 
                        primary={<Typography variant="body2" sx={{ fontWeight: 700 }}>{s.officialName}</Typography>}
                        secondary={
                          <Box component="span" sx={{ display: 'block' }}>
                             <Typography variant="caption" color="text.secondary" component="span" sx={{ display: 'block' }}>Cod: {s.materialCode} • {s.reason}</Typography>
                          </Box>
                        }
                      />
                      <Chip size="small" label={s.confidence} color={s.confidence === 'High' ? 'success' : 'default'} sx={{ height: 20, fontSize: '0.6rem' }} />
                    </ListItemButton>
                  ))}
                </List>
              </Box>
            )}

            {/* Busca Manual */}
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>BUSCAR NO CATÁLOGO</Typography>
              <Autocomplete
                fullWidth
                sx={{ mt: 1 }}
                options={searchResults}
                getOptionLabel={(o) => `[${o.materialCode}] ${o.officialName}`}
                loading={searchLoading}
                onInputChange={(_, val) => setSearchQuery(val)}
                onChange={(_, val) => setSelectedMaterial(val)}
                renderInput={(params) => <TextField {...params} size="small" placeholder="Nome, código ou GTIN..." />}
              />
            </Box>

            {selectedMaterial && (
              <Paper variant="outlined" sx={{ p: 2, bgcolor: alpha(theme.palette.primary.main, 0.02) }}>
                <Typography variant="caption" color="primary.main" sx={{ fontWeight: 800 }}>FATOR DE CONVERSÃO</Typography>
                <Stack direction="row" spacing={1} sx={{ mt: 1, alignItems: 'center' }}>
                  <Typography variant="body2">1 {item.supplierUnit} =</Typography>
                  <TextField 
                    type="number" 
                    size="small" 
                    sx={{ width: 80 }} 
                    value={conversionFactor} 
                    onChange={e => setConversionFactor(Number(e.target.value))}
                  />
                  <Typography variant="body2">{selectedMaterial.stockUnit || 'UN'}</Typography>
                </Stack>
                <Typography variant="caption" color="text.disabled" sx={{ mt: 1, display: 'block' }}>
                  Entrada no estoque: {(item.quantity * conversionFactor).toFixed(4)} {selectedMaterial.stockUnit || 'UN'}
                </Typography>
                
                <Button 
                  fullWidth 
                  variant="contained" 
                  sx={{ mt: 2, fontWeight: 800 }} 
                  startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <SaveIcon size={16} />}
                  disabled={isSubmitting || conversionFactor <= 0}
                  onClick={() => handleMapExisting(selectedMaterial)}
                >
                  Salvar Associação
                </Button>
              </Paper>
            )}
          </Stack>
        )}

        {tab === 'create' && (
          <CreateMaterialForm 
            tenantCode={tenantCode}
            receiptId={receiptId}
            item={item}
            onSuccess={onSuccess}
          />
        )}

        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}

        <Box sx={{ mt: 4 }}>
          <Divider />
          <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
            <Button 
              fullWidth 
              size="small" 
              startIcon={<ReviewLaterIcon size={14} />} 
              onClick={async () => {
                const reason = window.prompt("Motivo para revisar depois:");
                if (reason) {
                  try {
                    const res = await recordControlledDecision(tenantCode, receiptId, item.itemId, 'review-later', { expectedVersion: item.version, reason });
                    onSuccess(res);
                  } catch (err) { alert(err); }
                }
              }}
            >
              Revisar Depois
            </Button>
            <Button 
              fullWidth 
              size="small" 
              color="inherit" 
              startIcon={<IgnoreIcon size={14} />}
              onClick={async () => {
                const reason = window.prompt("Motivo para ignorar o item:");
                if (reason) {
                  try {
                    const res = await recordControlledDecision(tenantCode, receiptId, item.itemId, 'ignored', { expectedVersion: item.version, reason });
                    onSuccess(res);
                  } catch (err) { alert(err); }
                }
              }}
            >
              Ignorar Item
            </Button>
          </Stack>
        </Box>
      </Authorized>
    </Box>
  );
}

function CreateMaterialForm({ tenantCode, receiptId, item, onSuccess }: { 
  tenantCode: string, 
  receiptId: string, 
  item: WorkbenchItem, 
  onSuccess: (updated: any) => void 
}) {
  const [formData, setFormData] = useState({
    materialCode: item.supplierProductCode,
    officialName: item.originalDescription || item.description,
    description: item.description,
    unitOfMeasure: item.supplierUnit,
    procurementType: 'Buy',
    category: 'RawMaterial',
    ncm: item.ncm || '',
    gtin: item.gtin || '',
    conversionFactor: 1
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    setIsSubmitting(true);
    setError(null);
    try {
      const res = await createMaterialAndAssociate(tenantCode, receiptId, item.itemId, {
        expectedVersion: item.version,
        ...formData
      });
      onSuccess(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na criação');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Stack spacing={2}>
      <TextField label="SKU Interno" fullWidth size="small" value={formData.materialCode} onChange={e => setFormData({...formData, materialCode: e.target.value})} />
      <TextField label="Nome Oficial" fullWidth size="small" value={formData.officialName} onChange={e => setFormData({...formData, officialName: e.target.value})} />
      <Stack direction="row" spacing={2}>
        <TextField label="Unidade Base" sx={{ flex: 1 }} size="small" value={formData.unitOfMeasure} onChange={e => setFormData({...formData, unitOfMeasure: e.target.value})} />
        <TextField label="Fator Conv." type="number" sx={{ flex: 1 }} size="small" value={formData.conversionFactor} onChange={e => setFormData({...formData, conversionFactor: Number(e.target.value)})} />
      </Stack>
      <TextField 
        select 
        label="Categoria" 
        fullWidth 
        size="small" 
        value={formData.category} 
        onChange={e => setFormData({...formData, category: e.target.value})}
      >
        <MenuItem value="RawMaterial">Matéria-Prima</MenuItem>
        <MenuItem value="FinishedGood">Produto Acabado</MenuItem>
        <MenuItem value="Packaging">Embalagem</MenuItem>
        <MenuItem value="Consumable">Consumível</MenuItem>
      </TextField>
      <Stack direction="row" spacing={2}>
        <TextField label="NCM" sx={{ flex: 1 }} size="small" value={formData.ncm} onChange={e => setFormData({...formData, ncm: e.target.value})} />
        <TextField label="GTIN" sx={{ flex: 1 }} size="small" value={formData.gtin} onChange={e => setFormData({...formData, gtin: e.target.value})} />
      </Stack>
      <Button 
        variant="contained" 
        color="primary" 
        fullWidth 
        startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <AddIcon size={16} />}
        disabled={isSubmitting}
        onClick={handleSubmit}
        sx={{ fontWeight: 800 }}
      >
        Criar e Vincular
      </Button>
      {error && <Alert severity="error">{error}</Alert>}
    </Stack>
  );
}
