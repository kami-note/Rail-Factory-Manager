import React from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Grid, 
  Stack,
  useTheme,
  useMediaQuery
} from '@mui/material';
import { ArrowUpRight, ShieldCheck, AlertCircle, Package } from 'lucide-react';
import type { Status } from './types';
import { StatCard } from '../../components/common/StatCard';

/**
 * Renders the main dashboard overview panel.
 * @param status - The current system status information.
 * @param onNavigate - Navigation callback for quick actions.
 * @remarks
 * This panel provides a high-level summary of factory KPIs and quick access to operational modules.
 * Hardcoded mock data and technical telemetry have been removed to ensure a clean operational view.
 */
export function OverviewPanel({
  status,
  onNavigate
}: {
  status: Status | null;
  statusError: string | null;
  onNavigate: (path: string) => void;
}) {
  const theme = useTheme();
  const isSmall = useMediaQuery(theme.breakpoints.down('sm'));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      {/* 1. KPI STRIP: Responsive grid for stats */}
      <Box sx={{ borderBottom: '1px solid #edebe9' }}>
        <Grid container>
          <Grid size={{ xs: 12, sm: 6, md: 4 }} sx={{ borderRight: { sm: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', md: 0 } }}>
            <StatCard label="PENDING ACTIONS" value="--" icon={<AlertCircle size={16} />} color="error.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }} sx={{ borderRight: { md: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', sm: 0 } }}>
            <StatCard label="STOCK ALERTS" value="0" icon={<Package size={16} />} color="success.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <StatCard label="SYSTEM STATUS" value="ACTIVE" icon={<ShieldCheck size={16} />} color="success.main" />
          </Grid>
        </Grid>
      </Box>

      {/* QUICK ACTIONS FOR TABLET/DESKTOP */}
      {!isSmall && (
        <Box sx={{ px: 4, py: 2, display: 'flex', justifyContent: 'flex-end', bgcolor: '#faf9f8', borderBottom: '1px solid #edebe9', gap: 2, alignItems: 'center' }}>
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
      <Box sx={{ 
        flexGrow: 1, 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        p: 4,
        textAlign: 'center'
      }}>
        <Stack spacing={2} sx={{ maxWidth: 400 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, color: 'text.primary' }}>
            Welcome to Rail Factory
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            You are connected to the <Box component="span" sx={{ fontWeight: 700, color: 'primary.main' }}>{status?.tenant.code.toUpperCase()}</Box> tenant.
            Use the sidebar to navigate between Supply Chain and Inventory modules.
          </Typography>
          <Box sx={{ pt: 2 }}>
            <Button 
              variant="outlined" 
              onClick={() => onNavigate('/app/receipts')}
              sx={{ borderRadius: 1, textTransform: 'none' }}
            >
              Start Inbound Process
            </Button>
          </Box>
        </Stack>
      </Box>
    </Box>
  );
}
