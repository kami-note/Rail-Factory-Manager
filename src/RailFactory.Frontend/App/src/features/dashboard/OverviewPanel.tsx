import React from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Grid, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Divider,
  Stack,
  alpha
} from '@mui/material';
import { ArrowUpRight, Activity, ShieldCheck, AlertCircle, BarChart3, Package, History } from 'lucide-react';
import type { Status } from './types';

const lineRows = [
  { id: 'RF-101', product: 'Chassis A1 Heavy', priority: 'High', status: 'Active', color: '#107c10' },
  { id: 'RF-102', product: 'Brake System v2', priority: 'Normal', status: 'Idle', color: '#605e5c' },
  { id: 'RF-103', product: 'Control Unit C', priority: 'Critical', status: 'Warning', color: '#d13438' },
  { id: 'RF-104', product: 'Wheel Set 18in', priority: 'Low', status: 'Active', color: '#107c10' },
  { id: 'RF-105', product: 'Battery Pack L2', priority: 'High', status: 'Active', color: '#107c10' },
];

export function OverviewPanel({
  status,
  onNavigate
}: {
  status: Status | null;
  statusError: string | null;
  onNavigate: (path: string) => void;
}) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      {/* 1. KPI STRIP: Alinhamento preciso e ícones semânticos */}
      <Box sx={{ 
        display: 'flex', 
        borderBottom: '1px solid #edebe9',
        bgcolor: '#ffffff'
      }}>
        {[
          { label: 'PENDING ACTIONS', value: '12', icon: <AlertCircle size={16} />, color: '#d13438' },
          { label: 'OEE EFFICIENCY', value: '94.2%', icon: <Activity size={16} />, color: '#0078d4' },
          { label: 'DAILY OUTPUT', value: '1,204', icon: <Package size={16} />, color: '#107c10' },
          { label: 'HEALTH', value: 'NOMINAL', icon: <ShieldCheck size={16} />, color: '#107c10' }
        ].map((stat, idx) => (
          <Box key={idx} sx={{ 
            flex: 1, 
            p: 4, 
            borderRight: '1px solid #f3f2f1',
            '&:last-of-type': { borderRight: 0 }
          }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <Box sx={{ color: stat.color }}>{stat.icon}</Box>
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>{stat.label}</Typography>
                <Typography variant="h2" sx={{ lineHeight: 1 }}>{stat.value}</Typography>
              </Box>
            </Stack>
          </Box>
        ))}
        <Box sx={{ px: 4, display: 'flex', alignItems: 'center' }}>
          <Button 
            variant="contained" 
            disableElevation
            onClick={() => onNavigate('/app/receipts')}
            sx={{ height: 36, px: 3 }}
            endIcon={<ArrowUpRight size={16} />}
          >
            Manage Receipts
          </Button>
        </Box>
      </Box>

      {/* 2. MAIN WORKSPACE */}
      <Grid container sx={{ flexGrow: 1 }}>
        <Grid size={{ xs: 12, lg: 8 }} sx={{ borderRight: '1px solid #edebe9' }}>
          <Box sx={{ p: 2, px: 4, bgcolor: '#faf9f8', borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 2 }}>
            <BarChart3 size={14} color="#605e5c" />
            <Typography variant="caption" sx={{ color: '#323130', fontWeight: 700 }}>LIVE PRODUCTION MONITOR</Typography>
          </Box>
          <TableContainer>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>PRODUCT SPECIFICATION</TableCell>
                  <TableCell>PRIORITY</TableCell>
                  <TableCell>STATUS</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {lineRows.map((row, idx) => (
                  <TableRow key={row.id} sx={{ bgcolor: idx % 2 === 0 ? '#ffffff' : '#faf9f8' }}>
                    <TableCell sx={{ fontWeight: 700, color: 'primary.main' }}>{row.id}</TableCell>
                    <TableCell sx={{ color: 'text.primary', fontWeight: 500 }}>{row.product}</TableCell>
                    <TableCell sx={{ fontSize: '0.75rem', fontWeight: 600 }}>{row.priority.toUpperCase()}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={2} alignItems="center">
                        <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: row.color }} />
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>{row.status}</Typography>
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }} sx={{ display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ p: 2, px: 4, bgcolor: '#faf9f8', borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 2 }}>
            <History size={14} color="#605e5c" />
            <Typography variant="caption" sx={{ color: '#323130', fontWeight: 700 }}>ACTIVITY LOG</Typography>
          </Box>
          <Box sx={{ flexGrow: 1, overflow: 'auto' }}>
            {[
              { time: '10:45:02', msg: 'Batch RCPT-104 validation completed', type: 'success' },
              { time: '10:30:15', msg: 'Inbound Line #2 sensor recalibrated', type: 'info' },
              { time: '09:55:40', msg: 'System check: Storage capacity at 88%', type: 'warning' },
              { time: '08:45:12', msg: 'Shift handover successful (Team B)', type: 'info' }
            ].map((ev, i) => (
              <Box key={i} sx={{ 
                p: 2, 
                px: 4,
                borderBottom: '1px solid #f3f2f1',
                '&:hover': { bgcolor: '#f9fafb' }
              }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', display: 'block', mb: 0.5 }}>{ev.time}</Typography>
                <Typography variant="body2" sx={{ color: 'text.primary', fontWeight: 500 }}>{ev.msg}</Typography>
              </Box>
            ))}
          </Box>

          <Box sx={{ p: 4, borderTop: '1px solid #edebe9', bgcolor: '#faf9f8' }}>
            <Box sx={{ 
              p: 2, 
              bgcolor: '#1b1b1b', 
              borderRadius: 1,
              fontFamily: '"Cascadia Code", "Fira Code", monospace',
              fontSize: '0.7rem',
              color: '#9cdcfe'
            }}>
              <Box sx={{ color: '#6a9955', mb: 1 }}>// SYSTEM TELEMETRY</Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Box>
                  <Box sx={{ color: '#569cd6' }}>ENV: <span style={{ color: '#ce9178' }}>"{status?.environment || 'PROD'}"</span></Box>
                  <Box sx={{ color: '#569cd6' }}>SVC: <span style={{ color: '#ce9178' }}>"{status?.service || 'API'}"</span></Box>
                </Box>
                <Box sx={{ textAlign: 'right', color: '#b5cea8' }}>
                  LATENCY: 12ms<br />
                  NODE: 04_A
                </Box>
              </Box>
            </Box>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
}
