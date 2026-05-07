import React from 'react';
import { Box, Dialog, DialogContent, DialogTitle, IconButton, useTheme } from '@mui/material';
import { X } from 'lucide-react';

type ResponsiveCenteredModalProps = {
  open: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
};

export function ResponsiveCenteredModal({ open, title, onClose, children }: ResponsiveCenteredModalProps) {
  const theme = useTheme();

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="md"
      slotProps={{
        paper: {
          sx: {
            width: { xs: '94vw', sm: '90vw', md: 'min(920px, 92vw)' },
            maxWidth: 'none',
            m: { xs: 1, sm: 2, md: 3 },
            borderRadius: { xs: 2, md: 3 },
            borderTop: `5px solid ${theme.palette.primary.main}`
          }
        }
      }}
    >
      <DialogTitle sx={{ p: 3, pr: 8, fontWeight: 900 }}>
        {title}
        <IconButton
          onClick={onClose}
          size="large"
          sx={{ position: 'absolute', right: 16, top: 14, color: 'text.primary' }}
          aria-label="close modal"
        >
          <X size={28} />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers sx={{ p: { xs: 2, md: 4 } }}>
        <Box sx={{ maxHeight: { xs: '72vh', md: '70vh' }, overflow: 'auto' }}>
          {children}
        </Box>
      </DialogContent>
    </Dialog>
  );
}
