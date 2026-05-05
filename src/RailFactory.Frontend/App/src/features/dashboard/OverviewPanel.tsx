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
  Stack,
  useTheme,
  useMediaQuery
} from '@mui/material';
import { ArrowUpRight, Activity, ShieldCheck, AlertCircle, BarChart3, Package, History } from 'lucide-react';
import type { Status } from './types';
import { StatCard } from '../../components/common/StatCard';
import { ModuleHeader } from '../../components/common/ModuleHeader';
import { productionLines, activityLogs } from './mocks';

export function OverviewPanel({
  status,
  onNavigate
}: {
  status: Status | null;
  statusError: string | null;
  onNavigate: (path: string) => void;
}) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const isSmall = useMediaQuery(theme.breakpoints.down('sm'));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      {/* 1. KPI STRIP: Responsive grid for stats */}
      <Box sx={{ borderBottom: '1px solid #edebe9' }}>
        <Grid container>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { sm: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', md: 0 } }}>
            <StatCard label="PENDING ACTIONS" value="12" icon={<AlertCircle size={16} />} color="error.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { md: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', md: 0 } }}>
            <StatCard label="OEE EFFICIENCY" value="94.2%" icon={<Activity size={16} />} color="primary.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { sm: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', sm: 0 } }}>
            <StatCard label="DAILY OUTPUT" value="1,204" icon={<Package size={16} />} color="success.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <StatCard label="HEALTH" value="NOMINAL" icon={<ShieldCheck size={16} />} color="success.main" />
          </Grid>
        </Grid>
      </Box>

      {/* QUICK ACTIONS FOR TABLET/DESKTOP */}
      {!isSmall && (
        <Box sx={{ px: 4, py: 2, display: 'flex', justifyContent: 'flex-end', bgcolor: '#faf9f8', borderBottom: '1px solid #edebe9' }}>
          <Button 
            variant="contained" 
            disableElevation
            onClick={() => onNavigate('/app/receipts')}
            sx={{ height: 32, px: 3, fontSize: '0.75rem' }}
            endIcon={<ArrowUpRight size={14} />}
          >
            Manage Receipts
          </Button>
        </Box>
      )}

      {/* 2. MAIN WORKSPACE */}
      <Grid container sx={{ flexGrow: 1 }}>
        <Grid size={{ xs: 12, lg: 8 }} sx={{ borderRight: { lg: '1px solid #edebe9' }, borderBottom: { xs: '1px solid #edebe9', lg: 0 } }}>
          <ModuleHeader label="LIVE PRODUCTION MONITOR" icon={<BarChart3 size={14} />} />
          <TableContainer sx={{ maxHeight: { xs: 400, lg: 'unset' } }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>PRODUCT SPECIFICATION</TableCell>
                  <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>PRIORITY</TableCell>
                  <TableCell>STATUS</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {productionLines.map((row, idx) => (
                  <TableRow key={row.id} sx={{ bgcolor: idx % 2 === 0 ? '#ffffff' : '#faf9f8' }}>
                    <TableCell sx={{ fontWeight: 700, color: 'primary.main' }}>{row.id}</TableCell>
                    <TableCell sx={{ color: 'text.primary', fontWeight: 500 }}>{row.product}</TableCell>
                    <TableCell sx={{ fontSize: '0.75rem', fontWeight: 600, display: { xs: 'none', sm: 'table-cell' } }}>
                      {row.priority.toUpperCase()}
                    </TableCell>
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
          <ModuleHeader label="ACTIVITY LOG" icon={<History size={14} />} />
          <Box sx={{ flexGrow: 1, overflow: 'auto', maxHeight: { xs: 300, lg: 'unset' } }}>
            {activityLogs.map((ev, i) => (
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
                {!isSmall && (
                  <Box sx={{ textAlign: 'right', color: '#b5cea8' }}>
                    LATENCY: 12ms<br />
                    NODE: 04_A
                  </Box>
                )}
              </Box>
            </Box>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
}
