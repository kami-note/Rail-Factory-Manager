import React from 'react';
import {
  Box,
  Chip,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import type { Bom } from '../../types';
import { BomCard } from './BomCard';

export function ProductGroup({
  productCode,
  boms,
  tenantCode,
  expandedId,
  activatingId,
  onToggle,
  onActivate,
  onItemAdded,
  onClone,
  onCostRollup,
}: {
  productCode: string;
  boms: Bom[];
  tenantCode: string;
  expandedId: string | null;
  activatingId: string | null;
  onToggle: (id: string) => void;
  onActivate: (id: string) => void;
  onItemAdded: (bomId: string, updated: Bom) => void;
  onClone: (bomId: string) => void;
  onCostRollup: (bom: Bom) => void;
}) {
  const activeCount = boms.filter(b => b.status.key === 'Active').length;

  return (
    <Paper variant="outlined">
      <Box
        sx={{
          px: 2,
          py: 1.5,
          bgcolor: 'grey.50',
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Typography
            variant="subtitle2"
            sx={{ fontWeight: 800, fontFamily: 'monospace', flexGrow: 1 }}
          >
            {productCode}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {boms.length} {boms.length === 1 ? 'versão' : 'versões'}
          </Typography>
          {activeCount > 0 && (
            <Chip size="small" label="tem versão ativa" color="success" variant="outlined" />
          )}
        </Stack>
      </Box>

      <Stack>
        {boms.map((bom, idx) => (
          <Box
            key={bom.id}
            sx={{ borderTop: idx > 0 ? '1px solid' : 'none', borderColor: 'divider' }}
          >
            <BomCard
              bom={bom}
              tenantCode={tenantCode}
              expanded={expandedId === bom.id}
              activating={activatingId === bom.id}
              onToggle={() => onToggle(bom.id)}
              onActivate={() => onActivate(bom.id)}
              onItemAdded={updated => onItemAdded(bom.id, updated)}
              onClone={() => onClone(bom.id)}
              onCostRollup={() => onCostRollup(bom)}
            />
          </Box>
        ))}
      </Stack>
    </Paper>
  );
}
