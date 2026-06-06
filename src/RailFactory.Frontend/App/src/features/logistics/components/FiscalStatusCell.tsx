import React from 'react';
import { Box, Chip, Stack, Tooltip, Typography } from '@mui/material';
import { Copy, FileCheck2 } from 'lucide-react';
import { FISCAL_COLOR, FISCAL_LABEL } from '../types';
import type { FiscalStatus } from '../types';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';

const AUTHORIZED_STATUSES = new Set<string>(['autorizado', 'CONCLUIDO']);
const EMPTY = <Typography variant="caption" sx={{ color: '#bbb' }}>—</Typography>;

interface FiscalStatusCellProps {
  status?: FiscalStatus;
  accessKey?: string;
  externalId?: string;
  errorMessage?: string;
}

export function FiscalStatusCell({ status, accessKey, externalId, errorMessage }: FiscalStatusCellProps) {
  if (!status) return EMPTY;

  const color = FISCAL_COLOR[status] ?? 'default';
  const label = FISCAL_LABEL[status] ?? status;
  const isAuthorized = AUTHORIZED_STATUSES.has(status);
  const tooltipTitle = errorMessage ? `Erro SEFAZ: ${errorMessage}` : (externalId ? `ID Externo: ${externalId}` : '');

  const copyKey = () => { if (accessKey) void TechnicalIdFormatter.copyToClipboard(accessKey); };

  return (
    <Stack spacing={0.5} sx={{ alignItems: 'flex-start' }}>
      <Tooltip title={tooltipTitle} placement="top">
        <Chip
          icon={<FileCheck2 size={12} />}
          label={label}
          color={color}
          size="small"
          variant="outlined"
        />
      </Tooltip>
      {isAuthorized && accessKey && (
        <Tooltip title="Copiar chave de acesso">
          <Box
            onClick={copyKey}
            sx={{
              display: 'flex', alignItems: 'center', gap: 0.5,
              cursor: 'pointer', color: 'text.secondary',
              '&:hover': { color: 'primary.main' },
            }}
          >
            <Typography variant="caption" sx={{ fontFamily: 'monospace', fontSize: 10, lineHeight: 1 }}>
              {accessKey.slice(0, 10)}…{accessKey.slice(-4)}
            </Typography>
            <Copy size={10} />
          </Box>
        </Tooltip>
      )}
    </Stack>
  );
}
