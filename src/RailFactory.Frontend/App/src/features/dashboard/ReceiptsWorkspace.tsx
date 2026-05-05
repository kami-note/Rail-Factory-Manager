import React, { useEffect, useState } from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  IconButton, 
  Drawer, 
  Divider, 
  Stack,
  useTheme,
  useMediaQuery,
  Card
} from '@mui/material';
import { FileSpreadsheet, Plus, RefreshCcw, X } from 'lucide-react';
import { ImportXmlForm } from './ImportXmlForm';
import { NewReceiptForm } from './NewReceiptForm';
import { ReceiptsList } from './ReceiptsList';

type ReceiptDrawer = 'manual' | 'xml' | null;

type ReceiptsWorkspaceProps = {
  tenantCode: string;
  requestedDrawer?: ReceiptDrawer;
};

export function ReceiptsWorkspace({ tenantCode, requestedDrawer = null }: ReceiptsWorkspaceProps) {
  const [drawer, setDrawer] = useState<ReceiptDrawer>(requestedDrawer);
  const [refreshKey, setRefreshKey] = useState(0);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const isTablet = useMediaQuery(theme.breakpoints.down('md'));

  useEffect(() => {
    if (requestedDrawer) {
      setDrawer(requestedDrawer);
    }
  }, [requestedDrawer]);

  const handleCloseDrawer = () => setDrawer(null);

  const drawerTitle = drawer === 'manual' ? 'NEW MANUAL RECEIPT' : drawer === 'xml' ? 'IMPORT XML BATCH' : '';

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      {/* TOOLBAR INDUSTRIAL: Optimized for space */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: { xs: 'flex-start', md: 'center' }, 
        flexDirection: { xs: 'column', md: 'row' },
        gap: 3,
        mb: 4
      }}>
        <Box>
          <Typography variant="h1" sx={{ fontWeight: 900, mb: 0.5 }}>
            RECEIPTS
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
            MANAGE INBOUND MATERIALS
          </Typography>
        </Box>
        <Stack direction="row" spacing={2} sx={{ width: { xs: '100%', md: 'auto' }, justifyContent: 'flex-end' }}>
          <Button 
            variant="contained" 
            size={isTablet ? "medium" : "large"}
            startIcon={<Plus size={18} />} 
            onClick={() => setDrawer('manual')}
            sx={{ flexGrow: { xs: 1, md: 0 }, minWidth: { md: 160 } }}
          >
            NEW RECEIPT
          </Button>
          <Button 
            variant="outlined" 
            size={isTablet ? "medium" : "large"}
            startIcon={<FileSpreadsheet size={18} />} 
            onClick={() => setDrawer('xml')}
            sx={{ flexGrow: { xs: 1, md: 0 }, minWidth: { md: 160 }, borderWidth: 2, '&:hover': { borderWidth: 2 } }}
          >
            IMPORT XML
          </Button>
          <IconButton 
            size="medium" 
            onClick={() => setRefreshKey(current => current + 1)} 
            sx={{ bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }}
          >
            <RefreshCcw size={18} />
          </IconButton>
        </Stack>
      </Box>

      {/* LISTA PRINCIPAL */}
      <Box sx={{ mt: 1 }}>
        <ReceiptsList tenantCode={tenantCode} refreshKey={refreshKey} />
      </Box>

      {/* DRAWER PARA ENTRADA DE DADOS */}
      <Drawer
        anchor="right"
        open={drawer !== null}
        onClose={handleCloseDrawer}
        PaperProps={{
          sx: { 
            width: isMobile ? '100%' : isTablet ? '80%' : 550, 
            p: 0,
            borderLeft: `5px solid ${theme.palette.primary.main}`
          }
        }}
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
          <Box sx={{ p: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center', bgcolor: '#f8f9fa' }}>
            <Typography variant="h5" sx={{ fontWeight: 900 }}>
              {drawerTitle}
            </Typography>
            <IconButton onClick={handleCloseDrawer} size="large" sx={{ color: 'text.primary' }}>
              <X size={32} />
            </IconButton>
          </Box>
          <Divider />
          <Box sx={{ p: 4, flexGrow: 1, overflow: 'auto' }}>
            {drawer === 'manual' ? <NewReceiptForm tenantCode={tenantCode} showTitle={false} /> : null}
            {drawer === 'xml' ? <ImportXmlForm tenantCode={tenantCode} showTitle={false} /> : null}
          </Box>
        </Box>
      </Drawer>
    </Box>
  );
}
