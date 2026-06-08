import React, { useEffect, useState } from 'react';
import {
  Accordion, AccordionDetails, AccordionSummary, Alert,
  Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, Grid, IconButton, Stack, TextField, Typography, CircularProgress,
} from '@mui/material';
import { Button } from '@mui/material';
import { ChevronDown, PackagePlus, X } from 'lucide-react';
import { createShipmentOrder, addShipmentItem } from '../api/logistics';
import type { ShipmentItem, ShipmentOrder } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialCodeAutocomplete } from '../../inventory/components/MaterialCodeAutocomplete';
import type { MaterialSearchResult } from '../../inventory/types';
import { CurrencyFormatter } from '../../../shared/lib/utils/formatters';
import { Masks, Validators } from '../../../shared/lib/utils/masks';

type Props = { open: boolean; tenantCode: string; onCreated: (o: ShipmentOrder) => void; onClose: () => void };

type Phase = 'header' | 'items';

interface HeaderForm {
  notes: string;
  recipientCnpj: string; recipientName: string; recipientEmail: string;
  recipientStreet: string; recipientNumber: string; recipientDistrict: string;
  recipientCity: string; recipientState: string; recipientZipCode: string;
  recipientIe: string;
  modalidadeFrete: string;
  natureOfOperation: string;
}

interface ItemForm {
  materialCode: string;
  quantity: string;
  unitOfMeasure: string;
  weightKg: string;
  volumeCbm: string;
  ncmCode: string;
  cfopCode: string;
  unitValue: string;
  taxBaseIcms: string;
  icmsRate: string;
  icmsOrigin: string;
  icmsCst: string;
  pisCst: string;
  cofinsCst: string;
  ipiRate: string;
  ipiCst: string;
}

const EMPTY_HEADER: HeaderForm = {
  notes: '',
  recipientCnpj: '', recipientName: '', recipientEmail: '',
  recipientStreet: '', recipientNumber: '', recipientDistrict: '',
  recipientCity: '', recipientState: '', recipientZipCode: '',
  recipientIe: '', modalidadeFrete: '0',
  natureOfOperation: 'Venda de mercadoria',
};

const EMPTY_ITEM: ItemForm = {
  materialCode: '', quantity: '', unitOfMeasure: 'UN',
  weightKg: '', volumeCbm: '',
  ncmCode: '', cfopCode: '5102', unitValue: '', taxBaseIcms: '', icmsRate: '12',
  icmsOrigin: '0', icmsCst: '40', pisCst: '07', cofinsCst: '07', ipiRate: '0', ipiCst: '99',
};

export function CreateShipmentOrderModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [phase, setPhase] = useState<Phase>('header');
  const [header, setHeader] = useState<HeaderForm>(EMPTY_HEADER);
  const [createdOrder, setCreatedOrder] = useState<ShipmentOrder | null>(null);
  const [items, setItems] = useState<ShipmentItem[]>([]);

  const [itemForm, setItemForm] = useState<ItemForm>(EMPTY_ITEM);
  const [saving, setSaving] = useState(false);
  const [addingItem, setAddingItem] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [itemError, setItemError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setPhase('header'); setHeader(EMPTY_HEADER);
      setCreatedOrder(null); setItems([]);
      setItemForm(EMPTY_ITEM);
      setSaving(false); setAddingItem(false);
      setError(null); setItemError(null);
    }
  }, [open]);

  const setH = (field: keyof HeaderForm) => (e: React.ChangeEvent<HTMLInputElement>) => {
    let val = e.target.value;
    if (field === 'recipientCnpj') {
      val = Masks.cpfCnpj(val);
    } else if (field === 'recipientZipCode') {
      val = Masks.cep(val);
    }
    setHeader(prev => ({ ...prev, [field]: val }));
  };

  const setI = (field: keyof ItemForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setItemForm(prev => ({ ...prev, [field]: e.target.value }));

  const handleMaterialSelect = (m: MaterialSearchResult) => {
    setItemForm(prev => ({
      ...prev,
      materialCode: m.materialCode,
      unitOfMeasure: m.unitOfMeasure ?? prev.unitOfMeasure,
      ncmCode: m.ncm ?? prev.ncmCode,
    }));
  };

  const isCnpjValid = !header.recipientCnpj || Validators.cpfCnpj(header.recipientCnpj);
  const isEmailValid = !header.recipientEmail || Validators.email(header.recipientEmail);
  const isZipValid = !header.recipientZipCode || Validators.cep(header.recipientZipCode);
  const isStateValid = !header.recipientState || header.recipientState.length === 2;
  const isHeaderValid = isCnpjValid && isEmailValid && isZipValid && isStateValid;

  const handleCreateOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isHeaderValid || saving) return;
    setSaving(true); setError(null);
    try {
      const order = await createShipmentOrder(tenantCode, {
        notes: header.notes.trim() || undefined,
        recipientCnpj: header.recipientCnpj.trim() ? Masks.cleanDigits(header.recipientCnpj) : undefined,
        recipientName: header.recipientName.trim() || undefined,
        recipientEmail: header.recipientEmail.trim() || undefined,
        recipientStreet: header.recipientStreet.trim() || undefined,
        recipientNumber: header.recipientNumber.trim() || undefined,
        recipientDistrict: header.recipientDistrict.trim() || undefined,
        recipientCity: header.recipientCity.trim() || undefined,
        recipientState: header.recipientState.trim() || undefined,
        recipientZipCode: header.recipientZipCode.trim() ? Masks.cleanDigits(header.recipientZipCode) : undefined,
        recipientIe: header.recipientIe.trim() || undefined,
        modalidadeFrete: parseInt(header.modalidadeFrete || '0', 10),
        natureOfOperation: header.natureOfOperation.trim() || undefined,
      });
      setCreatedOrder(order);
      setPhase('items');
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar ordem de expedição.'));
    } finally {
      setSaving(false);
    }
  };

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();
    if (addingItem || !createdOrder) return;
    const qty = parseFloat(itemForm.quantity);
    if (isNaN(qty) || qty <= 0) { setItemError('Quantidade inválida.'); return; }
    setAddingItem(true); setItemError(null);
    try {
      const item = await addShipmentItem(tenantCode, createdOrder.id, {
        materialCode: itemForm.materialCode.trim().toUpperCase(),
        quantity: qty,
        unitOfMeasure: itemForm.unitOfMeasure.trim(),
        weightKg: parseFloat(itemForm.weightKg || '0'),
        volumeCbm: parseFloat(itemForm.volumeCbm || '0'),
        ncmCode: itemForm.ncmCode.trim() || undefined,
        cfopCode: itemForm.cfopCode.trim() || undefined,
        unitValue: parseFloat(itemForm.unitValue || '0'),
        taxBaseIcms: parseFloat(itemForm.taxBaseIcms || '0'),
        icmsRate: parseFloat(itemForm.icmsRate || '12'),
        icmsOrigin: parseInt(itemForm.icmsOrigin || '0', 10),
        icmsCst: itemForm.icmsCst.trim(),
        pisCst: itemForm.pisCst.trim(),
        cofinsCst: itemForm.cofinsCst.trim(),
        ipiRate: parseFloat(itemForm.ipiRate || '0'),
        ipiCst: itemForm.ipiCst.trim(),
      });
      setItems(prev => [...prev, item]);
      setItemForm(EMPTY_ITEM);
    } catch (err) {
      setItemError(toUiErrorMessage(err, 'Erro ao adicionar item.'));
    } finally {
      setAddingItem(false);
    }
  };

  const handleConclude = () => {
    if (!createdOrder) return;
    onCreated({ ...createdOrder, items });
  };

  const handleClose = () => { if (!saving && !addingItem) onClose(); };

  const isItemsPhase = phase === 'items';
  const totalValue = items.every(i => !i.unitValue) ? null
    : items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth={isItemsPhase ? 'md' : 'sm'}
      fullWidth
    >
      {/* ── FASE 1: Dados da ordem ── */}
      {phase === 'header' && (
        <form onSubmit={handleCreateOrder}>
          <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
            Nova Ordem de Expedição
            <IconButton onClick={handleClose} disabled={saving} size="small"><X size={18} /></IconButton>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {error && <Alert severity="error">{error}</Alert>}

              <TextField
                label="Observações (opcional)"
                value={header.notes}
                onChange={setH('notes')}
                fullWidth multiline rows={2} size="small"
              />

              <Accordion disableGutters elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                <AccordionSummary expandIcon={<ChevronDown size={16} />}>
                  <Typography variant="body2" fontWeight={700}>
                    Destinatário / Dados Fiscais NF-e
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <Stack spacing={2}>
                    <Typography variant="caption" color="text.secondary">
                      Preencha para habilitar emissão automática de NF-e ao despachar.
                    </Typography>
                    <Grid container spacing={1}>
                      <Grid xs={6}>
                        <TextField
                          label="CNPJ / CPF"
                          value={header.recipientCnpj}
                          onChange={setH('recipientCnpj')}
                          fullWidth
                          size="small"
                          placeholder="00.000.000/0000-00"
                          error={header.recipientCnpj.length > 0 && !isCnpjValid}
                          helperText={header.recipientCnpj.length > 0 && !isCnpjValid ? "CNPJ/CPF inválido" : ""}
                          slotProps={{ htmlInput: { maxLength: 18 } }}
                        />
                      </Grid>
                      <Grid xs={6}><TextField label="Razão Social / Nome" value={header.recipientName} onChange={setH('recipientName')} fullWidth size="small" /></Grid>
                      <Grid xs={12}>
                        <TextField
                          label="E-mail"
                          value={header.recipientEmail}
                          onChange={setH('recipientEmail')}
                          fullWidth
                          size="small"
                          error={header.recipientEmail.length > 0 && !isEmailValid}
                          helperText={header.recipientEmail.length > 0 && !isEmailValid ? "E-mail inválido" : ""}
                        />
                      </Grid>
                      <Grid xs={8}><TextField label="Logradouro" value={header.recipientStreet} onChange={setH('recipientStreet')} fullWidth size="small" /></Grid>
                      <Grid xs={4}><TextField label="Número" value={header.recipientNumber} onChange={setH('recipientNumber')} fullWidth size="small" /></Grid>
                      <Grid xs={6}><TextField label="Bairro" value={header.recipientDistrict} onChange={setH('recipientDistrict')} fullWidth size="small" /></Grid>
                      <Grid xs={6}>
                        <TextField
                          label="CEP"
                          value={header.recipientZipCode}
                          onChange={setH('recipientZipCode')}
                          fullWidth
                          size="small"
                          placeholder="00000-000"
                          error={header.recipientZipCode.length > 0 && !isZipValid}
                          helperText={header.recipientZipCode.length > 0 && !isZipValid ? "CEP inválido" : ""}
                          slotProps={{ htmlInput: { maxLength: 9 } }}
                        />
                      </Grid>
                      <Grid xs={8}><TextField label="Município" value={header.recipientCity} onChange={setH('recipientCity')} fullWidth size="small" /></Grid>
                      <Grid xs={4}>
                        <TextField
                          label="UF"
                          value={header.recipientState}
                          onChange={setH('recipientState')}
                          fullWidth
                          size="small"
                          placeholder="SP"
                          slotProps={{ htmlInput: { maxLength: 2, style: { textTransform: 'uppercase' } } }}
                        />
                      </Grid>
                      <Grid xs={4}><TextField label="IE Destinatário" value={header.recipientIe} onChange={setH('recipientIe')} fullWidth size="small" placeholder="Opcional" /></Grid>
                      <Grid xs={4}><TextField label="Modalidade Frete" value={header.modalidadeFrete} onChange={setH('modalidadeFrete')} fullWidth size="small" helperText="0=CIF 1=FOB 2=Terc 9=Sem" /></Grid>
                      <Grid xs={4}><TextField label="Natureza da Operação" value={header.natureOfOperation} onChange={setH('natureOfOperation')} fullWidth size="small" /></Grid>
                    </Grid>
                  </Stack>
                </AccordionDetails>
              </Accordion>
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
            <Button type="submit" variant="contained" disabled={saving || !isHeaderValid}>
              {saving ? <CircularProgress size={16} color="inherit" /> : 'Criar e Adicionar Itens →'}
            </Button>
          </DialogActions>
        </form>
      )}

      {/* ── FASE 2: Adicionar itens ── */}
      {phase === 'items' && createdOrder && (
        <>
          <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800, pb: 1 }}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <PackagePlus size={20} />
              <span>Itens da Ordem</span>
              <Typography variant="caption" sx={{ fontFamily: 'monospace', fontWeight: 700, bgcolor: 'action.hover', px: 1, py: 0.3, borderRadius: 1 }}>
                {createdOrder.orderNumber}
              </Typography>
            </Stack>
            <IconButton onClick={handleClose} size="small"><X size={18} /></IconButton>
          </DialogTitle>

          <DialogContent sx={{ pb: 1 }}>
            <Stack spacing={2.5} sx={{ mt: 0.5 }}>

              {/* Lista de itens já adicionados */}
              {items.length > 0 && (
                <Stack spacing={0.5}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                      Adicionados ({items.length})
                    </Typography>
                    {totalValue !== null && (
                      <Typography variant="caption" sx={{ fontWeight: 700 }}>
                        Total: {CurrencyFormatter.format(totalValue)}
                      </Typography>
                    )}
                  </Stack>
                  <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                    {items.map(item => (
                      <Chip
                        key={item.id}
                        size="small"
                        color="success"
                        variant="outlined"
                        label={`${item.materialCode} × ${item.quantity} ${item.unitOfMeasure}${item.unitValue ? ` · ${CurrencyFormatter.format(item.unitValue * item.quantity)}` : ''}`}
                      />
                    ))}
                  </Stack>
                </Stack>
              )}

              <Divider>
                <Typography variant="caption" color="text.secondary">
                  {items.length === 0 ? 'Adicione pelo menos um item' : 'Adicionar mais um item'}
                </Typography>
              </Divider>

              {/* Formulário inline de item */}
              <form id="add-item-form" onSubmit={handleAddItem}>
                <Stack spacing={2}>
                  {itemError && <Alert severity="error">{itemError}</Alert>}

                  {/* Material + Qtd + Valor */}
                  <Grid container spacing={1} sx={{ alignItems: 'flex-start' }}>
                    <Grid xs={12} sm={5}>
                      <MaterialCodeAutocomplete
                        tenantCode={tenantCode}
                        value={itemForm.materialCode}
                        onInputChange={v => setItemForm(prev => ({ ...prev, materialCode: v }))}
                        onMaterialSelect={handleMaterialSelect}
                        label="Material *"
                        fullWidth
                        size="small"
                      />
                    </Grid>
                    <Grid xs={6} sm={2.5}>
                      <TextField
                        label="Quantidade *"
                        value={itemForm.quantity}
                        onChange={setI('quantity')}
                        fullWidth size="small" required type="number"
                        slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                      />
                    </Grid>
                    <Grid xs={6} sm={2}>
                      <TextField
                        label="Unidade"
                        value={itemForm.unitOfMeasure}
                        onChange={setI('unitOfMeasure')}
                        fullWidth size="small"
                        slotProps={{ htmlInput: { style: { textTransform: 'uppercase' } } }}
                      />
                    </Grid>
                    <Grid xs={12} sm={2.5}>
                      <TextField
                        label="Valor Unit. (R$)"
                        value={itemForm.unitValue}
                        onChange={setI('unitValue')}
                        fullWidth size="small" type="number"
                        slotProps={{ htmlInput: { step: 0.01, min: 0 } }}
                      />
                    </Grid>
                  </Grid>

                  {/* Peso + Volume */}
                  <Grid container spacing={1}>
                    <Grid xs={6}>
                      <TextField
                        label="Peso (kg)"
                        value={itemForm.weightKg}
                        onChange={setI('weightKg')}
                        fullWidth size="small" type="number"
                        slotProps={{ htmlInput: { min: 0, step: 0.001 } }}
                      />
                    </Grid>
                    <Grid xs={6}>
                      <TextField
                        label="Volume (m³)"
                        value={itemForm.volumeCbm}
                        onChange={setI('volumeCbm')}
                        fullWidth size="small" type="number"
                        slotProps={{ htmlInput: { min: 0, step: 0.0001 } }}
                      />
                    </Grid>
                  </Grid>

                  {/* Dados fiscais — colapsado */}
                  <Accordion disableGutters elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                    <AccordionSummary expandIcon={<ChevronDown size={14} />} sx={{ minHeight: 36, '& .MuiAccordionSummary-content': { my: 0.5 } }}>
                      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                        Dados Fiscais NF-e (NCM, CFOP, CST…)
                      </Typography>
                    </AccordionSummary>
                    <AccordionDetails sx={{ pt: 1 }}>
                      <Grid container spacing={1}>
                        <Grid xs={4}><TextField label="NCM" value={itemForm.ncmCode} onChange={setI('ncmCode')} fullWidth size="small" placeholder="84713012" slotProps={{ htmlInput: { maxLength: 10 } }} /></Grid>
                        <Grid xs={4}><TextField label="CFOP" value={itemForm.cfopCode} onChange={setI('cfopCode')} fullWidth size="small" placeholder="5102" slotProps={{ htmlInput: { maxLength: 5 } }} /></Grid>
                        <Grid xs={4}><TextField label="Origem ICMS" value={itemForm.icmsOrigin} onChange={setI('icmsOrigin')} fullWidth size="small" type="number" /></Grid>
                        <Grid xs={3}><TextField label="CST ICMS" value={itemForm.icmsCst} onChange={setI('icmsCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                        <Grid xs={3}><TextField label="CST PIS" value={itemForm.pisCst} onChange={setI('pisCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                        <Grid xs={3}><TextField label="CST COFINS" value={itemForm.cofinsCst} onChange={setI('cofinsCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} /></Grid>
                        <Grid xs={3}><TextField label="Alíq. IPI (%)" value={itemForm.ipiRate} onChange={setI('ipiRate')} fullWidth size="small" type="number" /></Grid>
                        <Grid xs={3}><TextField label="CST IPI" value={itemForm.ipiCst} onChange={setI('ipiCst')} fullWidth size="small" slotProps={{ htmlInput: { maxLength: 3 } }} helperText="99=N.trib 50=Trib" /></Grid>
                        <Grid xs={4}><TextField label="Alíq. ICMS (%)" value={itemForm.icmsRate} onChange={setI('icmsRate')} fullWidth size="small" type="number" /></Grid>
                        <Grid xs={4}><TextField label="Base ICMS (R$)" value={itemForm.taxBaseIcms} onChange={setI('taxBaseIcms')} fullWidth size="small" type="number" /></Grid>
                      </Grid>
                    </AccordionDetails>
                  </Accordion>
                </Stack>
              </form>
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1 }}>
            <Button
              form="add-item-form"
              type="submit"
              variant="outlined"
              disabled={addingItem || !itemForm.materialCode || !itemForm.quantity}
              startIcon={addingItem ? <CircularProgress size={14} color="inherit" /> : undefined}
            >
              {addingItem ? 'Adicionando...' : '+ Adicionar Item'}
            </Button>
            <Button
              variant="contained"
              color="success"
              onClick={handleConclude}
              disabled={addingItem}
            >
              {items.length === 0 ? 'Concluir sem itens' : `Concluir (${items.length} ${items.length === 1 ? 'item' : 'itens'})`}
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
}
