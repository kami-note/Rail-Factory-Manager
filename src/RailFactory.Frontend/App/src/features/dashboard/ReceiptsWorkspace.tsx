import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { 
  Box, 
  Typography, 
  Button, 
  IconButton, 
  Stack
} from '@mui/material';
import { FileSpreadsheet, RefreshCcw } from 'lucide-react';
import { ResponsiveCenteredModal } from '../../components/ResponsiveCenteredModal';
import { ImportXmlForm } from './ImportXmlForm';
import { ReceiptsList } from './ReceiptsList';
import { ConferenceWorkspace } from './ConferenceWorkspace';

type ReceiptsWorkspaceProps = {
  tenantCode: string;
  requestedDrawer?: 'xml' | null;
};

export function ReceiptsWorkspace({ tenantCode, requestedDrawer = null }: ReceiptsWorkspaceProps) {
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeConferenceReceiptId, setActiveConferenceReceiptId] = useState<string | null>(null);
  const navigate = useNavigate();
  const location = useLocation();
  const isImportXmlRoute = location.pathname.endsWith('/import-xml');
  const isXmlModalOpen = requestedDrawer === 'xml' && isImportXmlRoute;

  const handleCloseModal = () => {
    if (isImportXmlRoute) {
      navigate('/app/receipts', { replace: true });
    }
  };

  const handleOpenXmlModal = () => {
    if (!isImportXmlRoute) {
      navigate('/app/import-xml');
    }
  };

  if (activeConferenceReceiptId) {
    return (
      <Box sx={{ p: { xs: 2, md: 4 } }}>
        <ConferenceWorkspace 
          receiptId={activeConferenceReceiptId} 
          tenantCode={tenantCode} 
          onClose={() => {
            setActiveConferenceReceiptId(null);
            setRefreshKey(prev => prev + 1);
          }} 
        />
      </Box>
    );
  }

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
            variant="outlined" 
            size="large"
            startIcon={<FileSpreadsheet size={18} />} 
            onClick={handleOpenXmlModal}
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
        <ReceiptsList 
          tenantCode={tenantCode} 
          refreshKey={refreshKey} 
          onStartConference={(id) => setActiveConferenceReceiptId(id)}
        />
      </Box>

      <ResponsiveCenteredModal
        open={isXmlModalOpen}
        onClose={handleCloseModal}
        title="IMPORT XML BATCH"
      >
        <ImportXmlForm tenantCode={tenantCode} showTitle={false} />
      </ResponsiveCenteredModal>
    </Box>
  );
}
