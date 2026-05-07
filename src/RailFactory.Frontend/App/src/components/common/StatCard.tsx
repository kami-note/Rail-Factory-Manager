import React from 'react';
import { Box, Stack, Typography, type Theme } from '@mui/material';

/**
 * Properties for the StatCard component.
 */
interface StatCardProps {
  label: string;
  value: string | number;
  icon: React.ReactNode;
  color?: string | ((theme: Theme) => string);
  /**
   * If true, shows a right border (desktop style).
   */
  hasDivider?: boolean;
}

/**
 * Renders a stylized KPI card for industrial dashboards.
 * @param props - Component properties.
 */
export const StatCard: React.FC<StatCardProps> = ({ label, value, icon, color, hasDivider }) => {
  return (
    <Box sx={{ 
      flex: 1, 
      p: { xs: 2, md: 4 }, 
      borderRight: hasDivider ? '1px solid #f3f2f1' : 0,
      '&:last-of-type': { borderRight: 0 }
    }}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
        <Box sx={{ color: color }}>{icon}</Box>
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5, textTransform: 'uppercase' }}>
            {label}
          </Typography>
          <Typography variant="h2" sx={{ lineHeight: 1 }}>
            {value}
          </Typography>
        </Box>
      </Stack>
    </Box>
  );
};
