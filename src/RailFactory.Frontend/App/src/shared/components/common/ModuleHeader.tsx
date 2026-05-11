import React from 'react';
import { Box, Typography } from '@mui/material';

/**
 * Properties for the ModuleHeader component.
 */
interface ModuleHeaderProps {
  /** The text label to display in the header. */
  label: string;
  /** The icon to display alongside the label. */
  icon: React.ReactNode;
  /** Optional action element (e.g., a button) to display on the right side. */
  action?: React.ReactNode;
}

/**
 * Renders a standardized header for modules and panels.
 */
export const ModuleHeader: React.FC<ModuleHeaderProps> = ({ label, icon, action }) => {
  return (
    <Box sx={{ 
      display: 'flex', 
      alignItems: 'center', 
      justifyContent: 'space-between',
      gap: 2,
      mb: 1
    }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <Box sx={{ color: '#605e5c', display: 'flex' }}>{icon}</Box>
        <Typography 
          variant="caption" 
          sx={{ 
            color: '#323130', 
            fontWeight: 700, 
            letterSpacing: '0.05em',
            textTransform: 'uppercase'
          }}
        >
          {label}
        </Typography>
      </Box>
      {action && <Box sx={{ display: 'flex' }}>{action}</Box>}
    </Box>
  );
};
