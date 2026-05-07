import React from 'react';
import { Box, Typography } from '@mui/material';

/**
 * Properties for the ModuleHeader component.
 */
interface ModuleHeaderProps {
  label: string;
  icon: React.ReactNode;
}

/**
 * Renders a consistent header for dashboard modules/panels.
 */
export const ModuleHeader: React.FC<ModuleHeaderProps> = ({ label, icon }) => {
  return (
    <Box sx={{ 
      p: 2, 
      px: 4, 
      bgcolor: '#faf9f8', 
      borderBottom: '1px solid #edebe9', 
      display: 'flex', 
      alignItems: 'center', 
      gap: 2 
    }}>
      <Box sx={{ color: '#605e5c', display: 'flex' }}>{icon}</Box>
      <Typography variant="caption" sx={{ color: '#323130', fontWeight: 700 }}>
        {label.toUpperCase()}
      </Typography>
    </Box>
  );
};
