import React from 'react';
import { Chip, Tooltip } from '@mui/material';
import { 
  HelpCircle as HelpIcon,
  CheckCircle2 as CheckCircleIcon,
  Plus as AddIcon,
  Clock as ReviewLaterIcon,
  Trash2 as IgnoreIcon,
  AlertCircle as ConflictIcon,
  AlertTriangle as WarningIcon,
  Play as RegisteredIcon,
  Settings as InConferenceIcon
} from 'lucide-react';

import type { DisplayStatus } from '../../lib/utils/status-mapping';

export type StatusType = 
  | 'Pending' | 'Mapped' | 'CreatedAndMapped' | 'ReviewLater' | 'Ignored' | 'Conflict'
  | 'Registered' | 'InConference' | 'Approved' | 'Divergent' | 'Cancelled'
  | 'Available' | 'Blocked' | 'Quarantine';

interface StatusChipProps {
  status: StatusType | DisplayStatus | string;
  label?: string;
  size?: 'small' | 'medium';
}

/**
 * Standardized chip for displaying operational and system statuses with consistent icons and colors.
 */
export const StatusChip: React.FC<StatusChipProps> = ({ status, label, size = 'small' }) => {
  const config: Record<string, { display: string, color: any, icon: any }> = {
    'Pending': { display: 'PENDING', color: 'warning', icon: <HelpIcon size={12} /> },
    'Mapped': { display: 'MAPPED', color: 'success', icon: <CheckCircleIcon size={12} /> },
    'CreatedAndMapped': { display: 'NEW', color: 'info', icon: <AddIcon size={12} /> },
    'ReviewLater': { display: 'REVIEW', color: 'info', icon: <ReviewLaterIcon size={12} /> },
    'Ignored': { display: 'IGNORED', color: 'default', icon: <IgnoreIcon size={12} /> },
    'Conflict': { display: 'CONFLICT', color: 'error', icon: <ConflictIcon size={12} /> },
    
    'Registered': { display: 'REGISTERED', color: 'info', icon: <RegisteredIcon size={12} /> },
    'InConference': { display: 'CONFERENCE', color: 'warning', icon: <InConferenceIcon size={12} /> },
    'Approved': { display: 'APPROVED', color: 'success', icon: <CheckCircleIcon size={12} /> },
    'Divergent': { display: 'DIVERGENT', color: 'error', icon: <WarningIcon size={12} /> },
    'Cancelled': { display: 'CANCELLED', color: 'default', icon: <IgnoreIcon size={12} /> },

    'Available': { display: 'AVAILABLE', color: 'success', icon: <CheckCircleIcon size={12} /> },
    'Blocked': { display: 'BLOCKED', color: 'error', icon: <ConflictIcon size={12} /> },
    'Quarantine': { display: 'QUARANTINE', color: 'warning', icon: <WarningIcon size={12} /> },
  };

  const statusKey = typeof status === 'string' ? status : (status as any).key || status;
  const item = config[statusKey] || { display: label || statusKey, color: 'default', icon: null };

  return (
    <Chip 
      size={size} 
      variant="outlined" 
      label={label || item.display} 
      color={item.color} 
      icon={item.icon} 
      sx={{ fontWeight: 800, fontSize: '0.65rem' }} 
    />
  );
};
