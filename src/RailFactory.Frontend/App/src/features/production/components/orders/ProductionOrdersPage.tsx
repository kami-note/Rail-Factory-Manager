import React, { useEffect, useMemo, useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import {
  ClipboardList,
  Plus,
  RefreshCw,
} from 'lucide-react';
import { ModuleHeader } from '../../../../shared/components/common/ModuleHeader';
import { InlineError } from '../../../../shared/components/common/InlineError';
import { StatusChip } from '../../../../shared/components/common/StatusChip';
import { Authorized } from '../../../auth';
import {
  listProductionOrders,
  listWorkCenters,
} from '../../api/production';
import type { ProductionOrder, WorkCenter } from '../../types';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import { OrderDetailDialog } from './OrderDetailDialog';
import { CreateOrderModal } from './CreateOrderModal';

const STATUS_OPTIONS = [
  { value: '', label: 'Todos' },
  { value: 'Draft', label: 'Rascunho' },
  { value: 'Released', label: 'Liberada' },
  { value: 'InExecution', label: 'Em Execução' },
  { value: 'Completed', label: 'Concluída' },
  { value: 'Cancelled', label: 'Cancelada' },
];

export function ProductionOrdersPage({ tenantCode }: { tenantCode: string }) {
  const [orders, setOrders] = useState<ProductionOrder[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [workCenterFilter, setWorkCenterFilter] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);

  const selectedOrder = useMemo(
    () => orders.find(o => o.id === selectedOrderId),
    [orders, selectedOrderId],
  );

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [ordersData, wcData] = await Promise.all([
        listProductionOrders(tenantCode, statusFilter || undefined, workCenterFilter || undefined),
        listWorkCenters(tenantCode),
      ]);
      setOrders(ordersData);
      setWorkCenters(wcData);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar as ordens de produção.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [tenantCode, statusFilter, workCenterFilter]);

  const updateOrderStatus = (id: string, status: ProductionOrder['status']) => {
    setOrders(prev => prev.map(o => o.id === id ? { ...o, status } : o));
  };

  const handleTransition = async (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => {
    setError(null);
    try {
      await action();
      updateOrderStatus(id, newStatus);
    } catch (err) {
      setError(toUiErrorMessage(err, 'A operação não pôde ser concluída.'));
    }
  };

  const workCenterName = (id: string) => workCenters.find(wc => wc.id === id)?.name ?? id.slice(0, 8);

  return (
    <Box sx={{ height: 'calc(100vh - 140px)', display: 'flex', flexDirection: 'column', p: 3 }}>
      <ModuleHeader
        label="ORDENS DE PRODUÇÃO"
        icon={<ClipboardList size={20} />}
        action={
          <Authorized permission="production.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setShowCreateModal(true)}>
              Nova Ordem
            </Button>
          </Authorized>
        }
      />

      {error && <InlineError message={error} marginBottom={2} />}

      <Stack direction="row" spacing={2} sx={{ my: 2, alignItems: 'center' }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Status</InputLabel>
          <Select label="Status" value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
            {STATUS_OPTIONS.map(s => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel>Centro de Trabalho</InputLabel>
          <Select label="Centro de Trabalho" value={workCenterFilter} onChange={e => setWorkCenterFilter(e.target.value)}>
            <MenuItem value="">Todos</MenuItem>
            {workCenters.filter(wc => wc.status.key === 'Active').map(wc => (
              <MenuItem key={wc.id} value={wc.id}>{wc.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <Button size="small" startIcon={<RefreshCw size={14} />} onClick={() => void load()}>
          Atualizar
        </Button>
      </Stack>

      <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
        {loading ? (
          <Box sx={{ textAlign: 'center', pt: 6 }}><CircularProgress size={32} /></Box>
        ) : (
          <TableContainer component={Paper} variant="outlined">
            <Table stickyHeader size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 800 }}>Nº ORDEM</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>PRODUTO</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>CENTRO</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 800 }}>QTD PLANEJADA</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {orders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                      Nenhuma ordem encontrada.
                    </TableCell>
                  </TableRow>
                ) : orders.map(order => (
                  <TableRow
                    key={order.id}
                    hover
                    onClick={() => setSelectedOrderId(order.id)}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{order.orderNumber}</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{order.productCode}</TableCell>
                    <TableCell sx={{ color: 'text.secondary' }}>{workCenterName(order.workCenterId)}</TableCell>
                    <TableCell align="right">{order.plannedQuantity}</TableCell>
                    <TableCell><StatusChip status={order.status} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Box>

      {selectedOrder && (
        <OrderDetailDialog
          key={selectedOrder.id}
          open={!!selectedOrderId}
          order={selectedOrder}
          workCenterName={workCenterName(selectedOrder.workCenterId)}
          tenantCode={tenantCode}
          onTransition={handleTransition}
          onClose={() => setSelectedOrderId(null)}
        />
      )}

      <CreateOrderModal
        open={showCreateModal}
        tenantCode={tenantCode}
        workCenters={workCenters}
        onCreated={order => { setOrders(prev => [order, ...prev]); setSelectedOrderId(order.id); }}
        onClose={() => setShowCreateModal(false)}
      />
    </Box>
  );
}
