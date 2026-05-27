import React from 'react';
import { Chip } from '@mui/material';
import {
  HelpCircle as HelpIcon,
  CheckCircle2 as CheckCircleIcon,
  Plus as AddIcon,
  Clock as ReviewLaterIcon,
  Trash2 as IgnoreIcon,
  AlertCircle as ConflictIcon,
  AlertTriangle as WarningIcon,
  Play as RegisteredIcon,
  Settings as InConferenceIcon,
  FileText as DraftIcon,
  Rocket as ReleasedIcon,
  Wrench as InExecutionIcon,
  Ban as InactiveIcon,
} from 'lucide-react';

import type { DisplayStatus } from '../../lib/utils/status-mapping';

export type StatusType =
  // Association Workbench
  | 'Pending' | 'Mapped' | 'CreatedAndMapped' | 'ReviewLater' | 'Ignored' | 'Conflict'
  // Supply Chain
  | 'Registered' | 'InConference' | 'Approved' | 'Divergent' | 'Cancelled'
  // Inventory
  | 'Available' | 'Blocked' | 'Quarantine'
  // Production — Work Center
  | 'Active' | 'Inactive'
  // Production — BOM & Production Order
  | 'Draft' | 'Released' | 'InExecution' | 'Completed';

interface StatusChipProps {
  /**
   * Either a raw status key string or a {@link DisplayStatus} object from the backend.
   * When a DisplayStatus object is provided, its `key` and `label` are used directly.
   */
  status: StatusType | DisplayStatus | string;
  /** Override the display label. When omitted, falls back to the mapped label or the key itself. */
  label?: string;
  size?: 'small' | 'medium';
}

/**
 * Standardized chip for displaying operational and system statuses with consistent icons and colors.
 * @remarks
 * Invariant: every status key returned by any API endpoint MUST have a corresponding entry here.
 * Add new entries when new domain statuses are introduced — never rely on the fallback `default` style.
 */
export const StatusChip: React.FC<StatusChipProps> = ({ status, label, size = 'small' }) => {
  const config: Record<string, { display: string, color: any, icon: any }> = {
    // ── Association Workbench ────────────────────────────────────────────────
    'Pending':        { display: 'PENDENTE',    color: 'warning', icon: <HelpIcon size={12} /> },
    'Mapped':         { display: 'MAPEADO',     color: 'success', icon: <CheckCircleIcon size={12} /> },
    'CreatedAndMapped': { display: 'NOVO',      color: 'info',    icon: <AddIcon size={12} /> },
    'ReviewLater':    { display: 'REVISAR',     color: 'info',    icon: <ReviewLaterIcon size={12} /> },
    'Ignored':        { display: 'IGNORADO',    color: 'default', icon: <IgnoreIcon size={12} /> },
    'Conflict':       { display: 'CONFLITO',    color: 'error',   icon: <ConflictIcon size={12} /> },

    // ── Supply Chain ─────────────────────────────────────────────────────────
    'Registered':     { display: 'REGISTRADO',  color: 'info',    icon: <RegisteredIcon size={12} /> },
    'InConference':   { display: 'CONFERÊNCIA', color: 'warning', icon: <InConferenceIcon size={12} /> },
    'Approved':       { display: 'APROVADO',    color: 'success', icon: <CheckCircleIcon size={12} /> },
    'Divergent':      { display: 'DIVERGENTE',  color: 'error',   icon: <WarningIcon size={12} /> },
    'Cancelled':      { display: 'CANCELADO',   color: 'default', icon: <IgnoreIcon size={12} /> },

    // ── Inventory ────────────────────────────────────────────────────────────
    'Available':      { display: 'DISPONÍVEL',  color: 'success', icon: <CheckCircleIcon size={12} /> },
    'Blocked':        { display: 'BLOQUEADO',   color: 'error',   icon: <ConflictIcon size={12} /> },
    'Quarantine':     { display: 'QUARENTENA',  color: 'warning', icon: <WarningIcon size={12} /> },

    // ── Production — Work Center ─────────────────────────────────────────────
    'Active':         { display: 'ATIVO',       color: 'success', icon: <CheckCircleIcon size={12} /> },
    'Inactive':       { display: 'INATIVO',     color: 'default', icon: <InactiveIcon size={12} /> },

    // ── Production — BOM & Production Order ──────────────────────────────────
    'Draft':          { display: 'RASCUNHO',    color: 'default', icon: <DraftIcon size={12} /> },
    'Released':       { display: 'LIBERADA',    color: 'info',    icon: <ReleasedIcon size={12} /> },
    'InExecution':    { display: 'EM EXECUÇÃO', color: 'warning', icon: <InExecutionIcon size={12} /> },
    'Completed':      { display: 'CONCLUÍDA',   color: 'success', icon: <CheckCircleIcon size={12} /> },
  };

  // Accepts both raw string keys and DisplayStatus objects from the backend.
  // When a DisplayStatus is provided, prefer its label over the hardcoded display string.
  const isDisplayStatus = typeof status === 'object' && status !== null && 'key' in status;
  const statusKey = isDisplayStatus ? (status as DisplayStatus).key : (status as string);
  const backendLabel = isDisplayStatus ? (status as DisplayStatus).label : undefined;

  const item = config[statusKey] ?? { display: label ?? backendLabel ?? statusKey, color: 'default', icon: null };

  return (
    <Chip
      size={size}
      variant="outlined"
      label={label ?? backendLabel ?? item.display}
      color={item.color}
      icon={item.icon}
      sx={{ fontWeight: 800, fontSize: '0.65rem' }}
    />
  );
};
