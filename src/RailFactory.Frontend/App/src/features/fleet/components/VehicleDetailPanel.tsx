import React, { useState } from 'react';
import {
  Box, Button, Chip, CircularProgress, Divider, Drawer,
  IconButton, Stack, Tab, Tabs, Table, TableBody, TableCell,
  TableHead, TableRow, Typography,
} from '@mui/material';
import { Fuel, UserCheck, UserX, Wrench, X } from 'lucide-react';
import { useDriverAssignments } from '../hooks/useDriverAssignments';
import { usePeople } from '../../hr/hooks/usePeople';
import { AssignDriverModal } from './AssignDriverModal';
import { MaintenanceContent } from './MaintenanceContent';
import { FuelingContent } from './FuelingContent';
import type { DriverAssignment, Vehicle } from '../types';

type Props = {
  vehicle: Vehicle;
  tenantCode: string;
  onClose: () => void;
};

const today = new Date().toISOString().slice(0, 10);

function isActive(a: DriverAssignment): boolean {
  return a.startDate <= today && (a.endDate == null || a.endDate >= today);
}

function DriverTab({ vehicle, tenantCode }: { vehicle: Vehicle; tenantCode: string }) {
  const { data: assignments, loading } = useDriverAssignments(tenantCode, vehicle.id);
  const { data: people } = usePeople(tenantCode);
  const [localAssignments, setLocalAssignments] = useState<DriverAssignment[] | null>(null);
  const [assignOpen, setAssignOpen] = useState(false);

  const all = localAssignments ?? assignments ?? [];
  const sorted = [...all].sort((a, b) => b.startDate.localeCompare(a.startDate));
  const active = sorted.find(isActive);
  const personName = (id: string) => people?.find(p => p.id === id)?.name ?? id;

  const handleAssigned = (a: DriverAssignment) => {
    setLocalAssignments(prev => [a, ...(prev ?? assignments ?? [])]);
    setAssignOpen(false);
  };

  return (
    <>
      <Stack spacing={2}>
        <Stack spacing={1}>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
            Motorista Ativo
          </Typography>
          {loading ? <CircularProgress size={20} /> : active ? (
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', p: 1.5, bgcolor: 'success.50', border: 1, borderColor: 'success.light', borderRadius: 1 }}>
              <UserCheck size={18} color="green" />
              <Box sx={{ flex: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>{personName(active.driverPersonId)}</Typography>
                <Typography variant="caption" color="text.secondary">
                  desde {new Date(active.startDate).toLocaleDateString('pt-BR')}
                  {active.endDate ? ` até ${new Date(active.endDate).toLocaleDateString('pt-BR')}` : ' — em aberto'}
                </Typography>
              </Box>
              <Chip label="Ativo" color="success" size="small" />
            </Stack>
          ) : (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', p: 1.5, bgcolor: 'warning.50', border: 1, borderColor: 'warning.light', borderRadius: 1 }}>
              <UserX size={18} />
              <Typography variant="body2" color="text.secondary">Nenhum motorista alocado.</Typography>
            </Stack>
          )}
        </Stack>

        <Divider />

        <Stack spacing={1}>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
            Histórico ({all.length})
          </Typography>
          {sorted.length === 0 ? (
            <Typography variant="body2" color="text.secondary">Nenhuma alocação registrada.</Typography>
          ) : (
            <Table size="small" sx={{ '& td, & th': { px: 1, py: 0.75 } }}>
              <TableHead>
                <TableRow sx={{ '& th': { fontWeight: 700, fontSize: 11, color: 'text.secondary', textTransform: 'uppercase' } }}>
                  <TableCell>Motorista</TableCell>
                  <TableCell>Início</TableCell>
                  <TableCell>Fim</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {sorted.map(a => (
                  <TableRow key={a.id} hover>
                    <TableCell sx={{ fontWeight: isActive(a) ? 700 : 400 }}>{personName(a.driverPersonId)}</TableCell>
                    <TableCell sx={{ fontSize: 12 }}>{new Date(a.startDate).toLocaleDateString('pt-BR')}</TableCell>
                    <TableCell sx={{ fontSize: 12, color: 'text.secondary' }}>{a.endDate ? new Date(a.endDate).toLocaleDateString('pt-BR') : '—'}</TableCell>
                    <TableCell>{isActive(a) && <Chip label="Ativo" color="success" size="small" />}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Stack>

        <Button
          variant="contained"
          startIcon={<UserCheck size={16} />}
          onClick={() => setAssignOpen(true)}
          disabled={vehicle.status.key !== 'active'}
          size="small"
        >
          Nova Alocação
        </Button>
        {vehicle.status.key !== 'active' && (
          <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center' }}>
            Ative o veículo para criar alocações.
          </Typography>
        )}
      </Stack>

      {assignOpen && (
        <AssignDriverModal
          vehicle={vehicle}
          tenantCode={tenantCode}
          onAssigned={handleAssigned}
          onClose={() => setAssignOpen(false)}
        />
      )}
    </>
  );
}

export function VehicleDetailPanel({ vehicle, tenantCode, onClose }: Props) {
  const [tab, setTab] = useState(0);

  return (
    <Drawer
      anchor="right"
      open
      onClose={onClose}
      sx={{ '& .MuiDrawer-paper': { width: { xs: '100%', sm: 500 } } }}
    >
      <Stack sx={{ height: '100%' }}>

        {/* Header */}
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', px: 2.5, py: 1.5, borderBottom: 1, borderColor: 'divider' }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <Typography variant="h6" sx={{ fontWeight: 800, fontFamily: 'monospace' }}>
              {vehicle.plate}
            </Typography>
            <Chip label={vehicle.type.label} size="small" />
            <Chip
              label={vehicle.status.label}
              color={vehicle.status.key === 'active' ? 'success' : 'default'}
              size="small"
            />
          </Stack>
          <IconButton onClick={onClose} size="small"><X size={18} /></IconButton>
        </Stack>

        {/* Tabs */}
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          sx={{ borderBottom: 1, borderColor: 'divider', minHeight: 40, px: 1 }}
        >
          <Tab label="Motorista" icon={<UserCheck size={14} />} iconPosition="start" sx={{ minHeight: 40, fontSize: 12 }} />
          <Tab label="Manutenção" icon={<Wrench size={14} />} iconPosition="start" sx={{ minHeight: 40, fontSize: 12 }} />
          <Tab label="Abastecimento" icon={<Fuel size={14} />} iconPosition="start" sx={{ minHeight: 40, fontSize: 12 }} />
        </Tabs>

        {/* Body */}
        <Box sx={{ flex: 1, overflowY: 'auto', px: 2.5, py: 2 }}>
          {tab === 0 && <DriverTab vehicle={vehicle} tenantCode={tenantCode} />}
          {tab === 1 && <MaintenanceContent tenantCode={tenantCode} vehicleId={vehicle.id} />}
          {tab === 2 && <FuelingContent tenantCode={tenantCode} vehicleId={vehicle.id} />}
        </Box>

      </Stack>
    </Drawer>
  );
}
