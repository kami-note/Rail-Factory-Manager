import React, { useState } from 'react';
import {
  Box,
  Typography,
  Stack,
  Button,
  IconButton,
  useTheme,
  useMediaQuery
} from '@mui/material';
import { FileSpreadsheet, RefreshCcw } from 'lucide-react';
import { useNavigate, useLocation } from 'react-router-dom';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { ImportXmlForm } from './ImportXmlForm';
import { ReceiptsList } from './ReceiptsList';
import { ConferenceWorkspace } from './ConferenceWorkspace';

type ReceiptsWorkspaceProps = {
  tenantCode: string;
  requestedDrawer?: 'xml' | null;
};

/**
 * Main workspace for managing Material Receipts.
 * Provides the list of receipts and the entry points for XML import and physical conference.
 * 
 * @param props - Component properties.
 */
export function ReceiptsWorkspace({ tenantCode, requestedDrawer = null }: ReceiptsWorkspaceProps) {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('sm'));
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
    navigate('/app/import-xml');
  };

  const handleRefresh = () => {
    setRefreshKey((prev) => prev + 1);
  };

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      {!activeConferenceReceiptId && (
        <Box sx={{ mb: 4 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} sx={{ justifyContent: 'space-between', alignItems: { xs: 'stretch', md: 'center' } }} spacing={2}>
            <Box>
              <Typography variant="h4" sx={{ fontWeight: 800, color: 'text.primary', letterSpacing: '-0.02em' }}>
                RECEBIMENTOS
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Gerencie notas fiscais de entrada e o status das conferências físicas.
              </Typography>
            </Box>
            <Stack direction="row" spacing={1} sx={{ alignSelf: { xs: 'stretch', md: 'center' } }}>
              <Button
                variant="contained"
                startIcon={<FileSpreadsheet size={18} />}
                onClick={handleOpenXmlModal}
                sx={{ fontWeight: 800, borderRadius: 2 }}
              >
                Importar XML
              </Button>
              <IconButton
                onClick={handleRefresh}
                sx={{
                  bgcolor: 'background.paper',
                  border: '1px solid',
                  borderColor: 'divider',
                  borderRadius: 2
                }}
              >
                <RefreshCcw size={18} />
              </IconButton>
            </Stack>
          </Stack>
        </Box>
      )}

      {activeConferenceReceiptId ? (
        <ConferenceWorkspace
          tenantCode={tenantCode}
          receiptId={activeConferenceReceiptId}
          onClose={() => setActiveConferenceReceiptId(null)}
          onSuccess={() => {
            setActiveConferenceReceiptId(null);
            setRefreshKey(prev => prev + 1);
          }}
        />
      ) : (
        <Box sx={{ mt: 1 }}>
          <ReceiptsList
            tenantCode={tenantCode}
            refreshKey={refreshKey}
            onStartConference={(id) => setActiveConferenceReceiptId(id)}
          />
        </Box>
      )}

      <ResponsiveCenteredModal
        open={isXmlModalOpen}
        onClose={handleCloseModal}
        title="IMPORTAR ARQUIVOS XML"
      >
        <ImportXmlForm tenantCode={tenantCode} showTitle={false} />
      </ResponsiveCenteredModal>
    </Box>
  );
}
