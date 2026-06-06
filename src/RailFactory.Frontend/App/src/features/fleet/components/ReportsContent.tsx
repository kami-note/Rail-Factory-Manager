import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Card, CardContent, Chip, CircularProgress, Divider,
  Paper, Stack, Tab, Tabs, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { Fuel, TrendingUp, UserCheck, Wrench } from 'lucide-react';
import { listMaintenancePlans, listFuelingRecords, type MaintenancePlan, type FuelingRecord } from '../api/fleet-maintenance';
import { listDriverAssignments } from '../api/fleet';
import { usePeople } from '../../hr/hooks/usePeople';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { DriverAssignment, Vehicle } from '../types/index';

type Props = {
  tenantCode: string;
  vehicles: Vehicle[];
};

type AllData = {
  plans: MaintenancePlan[];
  records: FuelingRecord[];
  assignments: DriverAssignment[];
};

const today = new Date().toISOString().slice(0, 10);
const firstDayOfMonth = today.slice(0, 7) + '-01';

function kpiCard(icon: React.ReactNode, label: string, value: string, sub?: string) {
  return (
    <Card variant="outlined" sx={{ flex: '1 1 160px', minWidth: 0 }}>
      <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
        <Stack spacing={0.5}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: 'text.secondary' }}>
            {icon}
            <Typography variant="caption" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
              {label}
            </Typography>
          </Stack>
          <Typography variant="h5" sx={{ fontWeight: 800, lineHeight: 1.2 }}>{value}</Typography>
          {sub && <Typography variant="caption" color="text.secondary">{sub}</Typography>}
        </Stack>
      </CardContent>
    </Card>
  );
}

// ── Consumo ───────────────────────────────────────────────────────────────────

function ConsumptionReport({ vehicles, records }: { vehicles: Vehicle[]; records: FuelingRecord[] }) {
  const rows = useMemo(() => {
    return vehicles.map(v => {
      const vr = records.filter(r => r.vehicleId === v.id);
      const liters = vr.reduce((s, r) => s + r.litersSupplied, 0);
      const cost = vr.reduce((s, r) => s + r.totalBrl, 0);
      const avgPpl = vr.length > 0 ? cost / liters : 0;
      const last = vr.sort((a, b) => b.date.localeCompare(a.date))[0]?.date ?? null;
      return { v, count: vr.length, liters, cost, avgPpl, last };
    }).sort((a, b) => b.cost - a.cost);
  }, [vehicles, records]);

  const totalLiters = rows.reduce((s, r) => s + r.liters, 0);
  const totalCost = rows.reduce((s, r) => s + r.cost, 0);
  const avgCostPerLiter = totalLiters > 0 ? totalCost / totalLiters : 0;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
        {kpiCard(<Fuel size={15} />, 'Total Litros', `${totalLiters.toFixed(0)} L`)}
        {kpiCard(<TrendingUp size={15} />, 'Custo Total', `R$ ${totalCost.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`, `Média R$ ${avgCostPerLiter.toFixed(3)}/L`)}
        {kpiCard(<Fuel size={15} />, 'Abastecimentos', `${records.length}`)}
      </Stack>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 800 }}>Veículo</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Registros</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Total (L)</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Custo Total (R$)</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Média R$/L</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Último</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.filter(r => r.count > 0).length === 0 ? (
              <TableRow><TableCell colSpan={6} align="center" sx={{ py: 3, color: 'text.secondary' }}>Sem registros no período.</TableCell></TableRow>
            ) : rows.map(({ v, count, liters, cost, avgPpl, last }) => count > 0 && (
              <TableRow key={v.id} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{v.plate}</TableCell>
                <TableCell align="right">{count}</TableCell>
                <TableCell align="right">{liters.toFixed(1)}</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700 }}>{cost.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}</TableCell>
                <TableCell align="right" sx={{ color: 'text.secondary' }}>{avgPpl.toFixed(3)}</TableCell>
                <TableCell sx={{ color: 'text.secondary', fontSize: 12 }}>{last ? new Date(last + 'T00:00:00').toLocaleDateString('pt-BR') : '—'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

// ── Manutenção ────────────────────────────────────────────────────────────────

function MaintenanceReport({ vehicles, plans }: { vehicles: Vehicle[]; plans: MaintenancePlan[] }) {
  const rows = useMemo(() => {
    return vehicles.map(v => {
      const vp = plans.filter(p => p.vehicleId === v.id);
      return {
        v,
        total: vp.length,
        scheduled: vp.filter(p => p.status === 'Scheduled').length,
        done: vp.filter(p => p.status === 'Done').length,
        cancelled: vp.filter(p => p.status === 'Cancelled').length,
        preventive: vp.filter(p => p.type === 'Preventive').length,
        corrective: vp.filter(p => p.type === 'Corrective').length,
      };
    }).sort((a, b) => b.total - a.total);
  }, [vehicles, plans]);

  const totalScheduled = plans.filter(p => p.status === 'Scheduled').length;
  const totalDone = plans.filter(p => p.status === 'Done').length;
  const totalCancelled = plans.filter(p => p.status === 'Cancelled').length;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
        {kpiCard(<Wrench size={15} />, 'Agendadas', `${totalScheduled}`, 'pendente de execução')}
        {kpiCard(<Wrench size={15} />, 'Concluídas', `${totalDone}`)}
        {kpiCard(<Wrench size={15} />, 'Canceladas', `${totalCancelled}`)}
      </Stack>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 800 }}>Veículo</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Total</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Agendadas</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Concluídas</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Canceladas</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Preventivas</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Corretivas</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.filter(r => r.total > 0).length === 0 ? (
              <TableRow><TableCell colSpan={7} align="center" sx={{ py: 3, color: 'text.secondary' }}>Sem registros no período.</TableCell></TableRow>
            ) : rows.map(({ v, total, scheduled, done, cancelled, preventive, corrective }) => total > 0 && (
              <TableRow key={v.id} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{v.plate}</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700 }}>{total}</TableCell>
                <TableCell align="right"><Chip label={scheduled} size="small" color={scheduled > 0 ? 'info' : 'default'} /></TableCell>
                <TableCell align="right"><Chip label={done} size="small" color={done > 0 ? 'success' : 'default'} /></TableCell>
                <TableCell align="right"><Chip label={cancelled} size="small" color={cancelled > 0 ? 'error' : 'default'} /></TableCell>
                <TableCell align="right" sx={{ color: 'text.secondary' }}>{preventive}</TableCell>
                <TableCell align="right" sx={{ color: 'text.secondary' }}>{corrective}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

// ── Motoristas ────────────────────────────────────────────────────────────────

function DriversReport({
  vehicles, assignments, tenantCode,
}: { vehicles: Vehicle[]; assignments: DriverAssignment[]; tenantCode: string }) {
  const { data: people } = usePeople(tenantCode);
  const driverName = (id: string) => people?.find(p => p.id === id)?.name ?? id.slice(0, 8) + '…';

  const rows = useMemo(() => {
    const byDriver = new Map<string, { plates: string[]; earliest: string; latest: string; activeVehicle: string | null }>();

    for (const a of assignments) {
      const plate = vehicles.find(v => v.id === a.vehicleId)?.plate ?? a.vehicleId.slice(0, 8);
      const existing = byDriver.get(a.driverPersonId);
      const isActive = a.startDate <= today && (a.endDate == null || a.endDate >= today);
      if (!existing) {
        byDriver.set(a.driverPersonId, {
          plates: [plate],
          earliest: a.startDate,
          latest: a.endDate ?? today,
          activeVehicle: isActive ? plate : null,
        });
      } else {
        if (!existing.plates.includes(plate)) existing.plates.push(plate);
        if (a.startDate < existing.earliest) existing.earliest = a.startDate;
        const end = a.endDate ?? today;
        if (end > existing.latest) existing.latest = end;
        if (isActive && !existing.activeVehicle) existing.activeVehicle = plate;
      }
    }

    return [...byDriver.entries()]
      .map(([id, d]) => ({ id, ...d }))
      .sort((a, b) => b.latest.localeCompare(a.latest));
  }, [assignments, vehicles]);

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
        {kpiCard(<UserCheck size={15} />, 'Motoristas', `${rows.length}`, 'com alocações no período')}
        {kpiCard(<UserCheck size={15} />, 'Ativos agora', `${rows.filter(r => r.activeVehicle).length}`)}
      </Stack>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 800 }}>Motorista</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Veículos</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Primeira alocação</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Última alocação</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.length === 0 ? (
              <TableRow><TableCell colSpan={5} align="center" sx={{ py: 3, color: 'text.secondary' }}>Sem alocações no período.</TableCell></TableRow>
            ) : rows.map(r => (
              <TableRow key={r.id} hover>
                <TableCell sx={{ fontWeight: 600 }}>{driverName(r.id)}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                    {r.plates.map(p => (
                      <Chip key={p} label={p} size="small" sx={{ fontFamily: 'monospace', fontSize: 11 }} />
                    ))}
                  </Stack>
                </TableCell>
                <TableCell sx={{ fontSize: 12 }}>{new Date(r.earliest + 'T00:00:00').toLocaleDateString('pt-BR')}</TableCell>
                <TableCell sx={{ fontSize: 12 }}>{new Date(r.latest + 'T00:00:00').toLocaleDateString('pt-BR')}</TableCell>
                <TableCell>
                  {r.activeVehicle
                    ? <Chip label={`Ativo — ${r.activeVehicle}`} color="success" size="small" />
                    : <Chip label="Inativo" size="small" />}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

// ── Visão Geral ───────────────────────────────────────────────────────────────

function OverviewReport({
  vehicles, plans, records, assignments,
}: { vehicles: Vehicle[]; plans: MaintenancePlan[]; records: FuelingRecord[]; assignments: DriverAssignment[] }) {
  const rows = useMemo(() => {
    return vehicles.map(v => {
      const vr = records.filter(r => r.vehicleId === v.id);
      const vp = plans.filter(p => p.vehicleId === v.id);
      const va = assignments.filter(a => a.vehicleId === v.id);
      const activeDriver = va.find(a => a.startDate <= today && (a.endDate == null || a.endDate >= today));
      return {
        v,
        liters: vr.reduce((s, r) => s + r.litersSupplied, 0),
        cost: vr.reduce((s, r) => s + r.totalBrl, 0),
        fuelings: vr.length,
        maintenanceDone: vp.filter(p => p.status === 'Done').length,
        maintenancePending: vp.filter(p => p.status === 'Scheduled').length,
        hasDriver: !!activeDriver,
      };
    }).sort((a, b) => a.v.plate.localeCompare(b.v.plate));
  }, [vehicles, plans, records, assignments]);

  const active = vehicles.filter(v => v.status.key === 'active').length;
  const withDriver = rows.filter(r => r.hasDriver).length;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
        {kpiCard(<Wrench size={15} />, 'Veículos ativos', `${active} / ${vehicles.length}`)}
        {kpiCard(<UserCheck size={15} />, 'Com motorista', `${withDriver}`)}
        {kpiCard(<Fuel size={15} />, 'Custo total (período)', `R$ ${rows.reduce((s, r) => s + r.cost, 0).toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`)}
      </Stack>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 800 }}>Placa</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Tipo</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>Motorista</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Abast.</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Litros</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Custo (R$)</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Manut. OK</TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>Manut. Pend.</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map(({ v, liters, cost, fuelings, maintenanceDone, maintenancePending, hasDriver }) => (
              <TableRow key={v.id} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{v.plate}</TableCell>
                <TableCell><Chip label={v.type.label} size="small" /></TableCell>
                <TableCell><Chip label={v.status.label} color={v.status.key === 'active' ? 'success' : 'default'} size="small" /></TableCell>
                <TableCell>
                  <Chip
                    label={hasDriver ? 'Com motorista' : 'Sem motorista'}
                    color={hasDriver ? 'success' : 'default'}
                    size="small"
                  />
                </TableCell>
                <TableCell align="right" sx={{ color: fuelings === 0 ? 'text.disabled' : 'inherit' }}>{fuelings}</TableCell>
                <TableCell align="right" sx={{ color: liters === 0 ? 'text.disabled' : 'inherit' }}>{liters > 0 ? liters.toFixed(1) : '—'}</TableCell>
                <TableCell align="right" sx={{ fontWeight: cost > 0 ? 700 : 400, color: cost === 0 ? 'text.disabled' : 'inherit' }}>
                  {cost > 0 ? cost.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '—'}
                </TableCell>
                <TableCell align="right">
                  {maintenanceDone > 0 ? <Chip label={maintenanceDone} size="small" color="success" /> : <span style={{ color: '#aaa' }}>—</span>}
                </TableCell>
                <TableCell align="right">
                  {maintenancePending > 0 ? <Chip label={maintenancePending} size="small" color="warning" /> : <span style={{ color: '#aaa' }}>—</span>}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

// ── Root ──────────────────────────────────────────────────────────────────────

export function ReportsContent({ tenantCode, vehicles }: Props) {
  const [tab, setTab] = useState(0);
  const [from, setFrom] = useState(firstDayOfMonth);
  const [to, setTo] = useState(today);
  const [data, setData] = useState<AllData | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    if (!vehicles.length) return;
    setLoading(true); setLoadError(null);
    Promise.all([
      Promise.all(vehicles.map(v => listMaintenancePlans(tenantCode, v.id))).then(r => r.flat()),
      Promise.all(vehicles.map(v => listFuelingRecords(tenantCode, v.id))).then(r => r.flat()),
      Promise.all(vehicles.map(v => listDriverAssignments(tenantCode, v.id))).then(r => r.flat()),
    ])
      .then(([plans, records, assignments]) => setData({ plans, records, assignments }))
      .catch(err => setLoadError(toUiErrorMessage(err, 'Erro ao carregar dados para relatórios.')))
      .finally(() => setLoading(false));
  }, [tenantCode, vehicles]);

  const filtered = useMemo(() => {
    if (!data) return null;
    return {
      plans: data.plans.filter(p => p.scheduledDate >= from && p.scheduledDate <= to),
      records: data.records.filter(r => r.date >= from && r.date <= to),
      assignments: data.assignments.filter(a => a.startDate <= to && (a.endDate == null || a.endDate >= from)),
    };
  }, [data, from, to]);

  if (loadError) return <Alert severity="error">{loadError}</Alert>;

  return (
    <Box>
      {/* Period filter */}
      <Stack direction="row" spacing={2} sx={{ mb: 3, alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.secondary' }}>PERÍODO</Typography>
        <TextField
          label="De"
          type="date"
          value={from}
          onChange={e => setFrom(e.target.value)}
          size="small"
          sx={{ width: 160, '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
        />
        <TextField
          label="Até"
          type="date"
          value={to}
          onChange={e => setTo(e.target.value)}
          size="small"
          sx={{ width: 160, '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
        />
        {loading && <CircularProgress size={18} />}
      </Stack>

      <Divider sx={{ mb: 2 }} />

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}>
        <Tab label="Visão Geral" icon={<Wrench size={14} />} iconPosition="start" />
        <Tab label="Consumo" icon={<Fuel size={14} />} iconPosition="start" />
        <Tab label="Manutenção" icon={<Wrench size={14} />} iconPosition="start" />
        <Tab label="Motoristas" icon={<UserCheck size={14} />} iconPosition="start" />
      </Tabs>

      {!filtered ? (
        <Box sx={{ py: 6, textAlign: 'center' }}><CircularProgress size={28} /></Box>
      ) : (
        <>
          {tab === 0 && <OverviewReport vehicles={vehicles} plans={filtered.plans} records={filtered.records} assignments={filtered.assignments} />}
          {tab === 1 && <ConsumptionReport vehicles={vehicles} records={filtered.records} />}
          {tab === 2 && <MaintenanceReport vehicles={vehicles} plans={filtered.plans} />}
          {tab === 3 && <DriversReport vehicles={vehicles} assignments={filtered.assignments} tenantCode={tenantCode} />}
        </>
      )}
    </Box>
  );
}
