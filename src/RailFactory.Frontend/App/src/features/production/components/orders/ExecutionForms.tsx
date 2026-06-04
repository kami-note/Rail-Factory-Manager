import React, { useState } from 'react';
import {
  Box,
  Collapse,
  IconButton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import { CheckCircle, ChevronDown, ChevronUp, FlaskConical, Trash2 } from 'lucide-react';
import { MaterialExecutionForm } from './MaterialExecutionForm';
import { InspectionForm } from './InspectionForm';
import type { BomItem } from '../../types';

export function ExecutionForms({ tenantCode, orderId, plannedQuantity, bomItems, onRecorded }: {
  tenantCode: string;
  orderId: string;
  plannedQuantity: number;
  bomItems: BomItem[];
  onRecorded: () => void;
}) {
  const [subTab, setSubTab] = useState(0);
  const [bomExpanded, setBomExpanded] = useState(true);

  return (
    <>
      {bomItems.length > 0 && (
        <Box sx={{ mb: 2, border: '1px solid #edebe9', borderRadius: 1, overflow: 'hidden' }}>
          <Box
            sx={{ px: 2, py: 1, bgcolor: '#faf9f8', display: 'flex', justifyContent: 'space-between', alignItems: 'center', cursor: 'pointer' }}
            onClick={() => setBomExpanded(prev => !prev)}
          >
            <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
              Itens da BOM ({bomItems.length})
            </Typography>
            <Tooltip title={bomExpanded ? 'Recolher' : 'Expandir'}>
              <IconButton size="small">
                {bomExpanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
              </IconButton>
            </Tooltip>
          </Box>
          <Collapse in={bomExpanded}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ bgcolor: '#faf9f8' }}>
                  <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem' }}>MATERIAL</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700, fontSize: '0.7rem' }}>QTD TOTAL</TableCell>
                  <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem' }}>UM</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bomItems.map(item => (
                  <TableRow key={item.id}>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem', fontWeight: 600 }}>
                      {item.materialCode}
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700 }}>
                      {(item.quantity * plannedQuantity).toLocaleString('pt-BR', { maximumFractionDigits: 4 })}
                    </TableCell>
                    <TableCell sx={{ color: 'text.secondary', fontSize: '0.75rem' }}>{item.unitOfMeasure}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Collapse>
        </Box>
      )}

      <Tabs value={subTab} onChange={(_, v) => setSubTab(v as number)} variant="fullWidth" sx={{ mb: 2 }}>
        <Tab label="Consumo" icon={<FlaskConical size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Scrap" icon={<Trash2 size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Inspeção" icon={<CheckCircle size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
      </Tabs>
      {subTab === 0 && <MaterialExecutionForm tenantCode={tenantCode} orderId={orderId} mode="consumption" onRecorded={onRecorded} />}
      {subTab === 1 && <MaterialExecutionForm tenantCode={tenantCode} orderId={orderId} mode="scrap" onRecorded={onRecorded} />}
      {subTab === 2 && <InspectionForm tenantCode={tenantCode} orderId={orderId} onRecorded={onRecorded} />}
    </>
  );
}
