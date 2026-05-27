import React, { useState } from 'react';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Divider,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
} from '@mui/material';
import { ChevronDown, ChevronRight, Plus, Zap } from 'lucide-react';
import { StatusChip } from '../../../../shared/components/common/StatusChip';
import { Authorized } from '../../../auth';
import type { Bom } from '../../types';
import { AddBomItemForm } from './AddBomItemForm';

export function BomCard({
  bom,
  tenantCode,
  expanded,
  activating,
  onToggle,
  onActivate,
  onItemAdded,
}: {
  bom: Bom;
  tenantCode: string;
  expanded: boolean;
  activating: boolean;
  onToggle: () => void;
  onActivate: () => void;
  onItemAdded: (updated: Bom) => void;
}) {
  const [showAddItem, setShowAddItem] = useState(false);

  return (
    <>
      <Stack
        direction="row"
        sx={{
          px: 2,
          py: 1.5,
          alignItems: 'center',
          cursor: 'pointer',
          userSelect: 'none',
          '&:hover': { bgcolor: 'action.hover' },
        }}
        onClick={onToggle}
      >
        <IconButton size="small" sx={{ mr: 1 }}>
          {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        </IconButton>
        <Stack direction="row" spacing={2} sx={{ flexGrow: 1, alignItems: 'center' }}>
          <Chip size="small" label={`v${bom.version}`} variant="outlined" />
          <StatusChip status={bom.status} />
          <Chip
            size="small"
            label={`${bom.items.length} ${bom.items.length === 1 ? 'componente' : 'componentes'}`}
            variant="outlined"
            sx={{ color: 'text.secondary', borderColor: 'divider' }}
          />
        </Stack>
        {bom.status.key === 'Draft' && (
          <Authorized permission="production.write">
            <Tooltip title="Ativar esta versão">
              <Button
                size="small"
                variant="contained"
                color="success"
                startIcon={
                  activating ? (
                    <CircularProgress size={14} color="inherit" />
                  ) : (
                    <Zap size={14} />
                  )
                }
                onClick={e => {
                  e.stopPropagation();
                  onActivate();
                }}
                disabled={activating}
                sx={{ fontWeight: 800 }}
              >
                Ativar
              </Button>
            </Tooltip>
          </Authorized>
        )}
      </Stack>

      <Collapse in={expanded}>
        <Divider />
        <Box sx={{ p: 2 }}>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>UNIDADE</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bom.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={3} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                      Sem componentes. Adicione itens abaixo.
                    </TableCell>
                  </TableRow>
                ) : (
                  bom.items.map(item => (
                    <TableRow key={item.id}>
                      <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
                        {item.materialCode}
                      </TableCell>
                      <TableCell align="right">{item.quantity}</TableCell>
                      <TableCell>{item.unitOfMeasure}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>

          {bom.status.key === 'Draft' && (
            <Authorized permission="production.write">
              <Box sx={{ mt: 2 }}>
                {showAddItem ? (
                  <AddBomItemForm
                    tenantCode={tenantCode}
                    bom={bom}
                    onAdded={updated => {
                      onItemAdded(updated);
                      setShowAddItem(false);
                    }}
                    onCancel={() => setShowAddItem(false)}
                  />
                ) : (
                  <Button
                    size="small"
                    startIcon={<Plus size={14} />}
                    onClick={() => setShowAddItem(true)}
                  >
                    Adicionar Componente
                  </Button>
                )}
              </Box>
            </Authorized>
          )}
        </Box>
      </Collapse>
    </>
  );
}
