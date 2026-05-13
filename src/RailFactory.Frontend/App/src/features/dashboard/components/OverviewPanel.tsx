import React from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Grid, 
  Stack,
  useTheme,
  useMediaQuery,
  alpha
} from '@mui/material';
import { ArrowUpRight, ShieldCheck, AlertCircle, Package } from 'lucide-react';
import type { Status } from '../types';
import { StatCard } from '../../../shared/components/common/StatCard';

/**
 * Renders the main dashboard overview panel.
 * @param status - The current system status information.
 * @param onNavigate - Navigation callback for quick actions.
 * @remarks
 * This panel provides a high-level summary of factory KPIs and quick access to operational modules.
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
            <StatCard label="AÇÕES PENDENTES" value="--" icon={<AlertCircle size={16} />} color="error.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }} sx={{ borderRight: { md: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', sm: 0 } }}>
            <StatCard label="ALERTAS DE ESTOQUE" value="0" icon={<Package size={16} />} color="success.main" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <StatCard label="STATUS DO SISTEMA" value="ATIVO" icon={<ShieldCheck size={16} />} color="success.main" />
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
            sx={{ height: 32, px: 3, fontSize: '0.75rem', fontWeight: 800, borderRadius: 2 }}
            endIcon={<ArrowUpRight size={14} />}
          >
            Gerenciar Recebimentos
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
        textAlign: 'center',
        bgcolor: alpha(theme.palette.primary.main, 0.01)
      }}>
        <Stack spacing={2} sx={{ maxWidth: 500 }}>
          <Typography variant="h4" sx={{ fontWeight: 900, color: 'text.primary', letterSpacing: '-0.04em' }}>
            Bem-vindo ao Rail Factory
          </Typography>
          <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.6 }}>
            Você está conectado ao tenant <Box component="span" sx={{ fontWeight: 800, color: 'primary.main' }}>{status?.tenant.code.toUpperCase()}</Box>.
            <br />
            Utilize a barra lateral para navegar entre os módulos de <strong>Recebimento</strong> e <strong>Estoque</strong>.
          </Typography>
          <Box sx={{ pt: 3 }}>
            <Button 
              variant="outlined" 
              size="large"
              onClick={() => onNavigate('/app/receipts')}
              sx={{ borderRadius: 2, textTransform: 'none', fontWeight: 800, px: 4 }}
            >
              Iniciar Processo de Entrada
            </Button>
          </Box>
        </Stack>
      </Box>
    </Box>
  );
}
