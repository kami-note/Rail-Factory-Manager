import React from 'react';
import { 
  Box, 
  Typography, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Paper, 
  Grid,
  Divider,
  Chip
} from '@mui/material';
import { Building2, FileText, Calendar, Hash } from 'lucide-react';

export type ParsedReceiptItem = {
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  unitPrice?: number;
  totalPrice?: number;
  originalDescription?: string;
  ncm?: string;
  cfop?: string;
  ean?: string;
  purchaseOrderNumber?: string;
  purchaseOrderItemNumber?: number;
};

export type ParsedReceiptDocument = {
  receiptNumber: string;
  documentNumber: string;
  accessKey?: string;
  totalValue?: number;
  operationNature?: string;
  receiptDate: string;
  supplierFiscalId: string;
  supplierName: string;
  items: ParsedReceiptItem[];
};

type FiscalDocumentPreviewProps = {
  /** The parsed document data to display. */
  data: ParsedReceiptDocument;
};

/**
 * Renders a visual summary of a parsed fiscal document (NF-e).
 * @param props - Component properties.
 * @remarks
 * This component is used during the "Import XML" phase to allow users to verify 
 * document integrity before final processing in the Supply Chain domain.
 * It strictly adheres to high-density layouts for quick visual auditing.
 */
export function FiscalDocumentPreview({ data }: FiscalDocumentPreviewProps) {
  if (!data) return null;

  const items = data.items || [];

  return (
    <Box sx={{ mt: 2 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2, color: 'primary.main', display: 'flex', alignItems: 'center', gap: 1 }}>
        <FileText size={16} /> FISCAL DOCUMENT SUMMARY
      </Typography>

      <Paper variant="outlined" sx={{ p: 3, bgcolor: 'background.default', mb: 3 }}>
        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 6 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Building2 size={20} style={{ marginTop: 2, opacity: 0.7 }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase' }}>
                  Supplier
                </Typography>
                <Typography variant="body1" sx={{ fontWeight: 700 }}>
                  {data.supplierName}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  CNPJ: {data.supplierFiscalId}
                </Typography>
              </Box>
            </Box>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Calendar size={20} style={{ marginTop: 2, opacity: 0.7 }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase' }}>
                  Issue Date
                </Typography>
                <Typography variant="body1" sx={{ fontWeight: 700 }}>
                  {data.receiptDate ? new Date(data.receiptDate).toLocaleDateString() : 'N/A'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Ref: {data.documentNumber}
                </Typography>
                {data.operationNature && (
                  <Typography variant="caption" color="text.secondary">
                    Operation: {data.operationNature}
                  </Typography>
                )}
              </Box>
            </Box>
          </Grid>
          <Grid size={12}>
            <Divider sx={{ my: 1, borderStyle: 'dashed' }} />
          </Grid>
          <Grid size={{ xs: 12, md: 8 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Hash size={20} style={{ marginTop: 2, opacity: 0.7 }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase' }}>
                  NF-e Access Key
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 500, fontFamily: 'monospace', letterSpacing: 1 }}>
                  {data.accessKey || 'N/A'}
                </Typography>
              </Box>
            </Box>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }} sx={{ textAlign: { md: 'right' } }}>
            <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase' }}>
              Total Value
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 900, color: 'success.main' }}>
              {data.totalValue !== undefined && data.totalValue !== null
                ? `R$ ${data.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
                : 'N/A'}
            </Typography>
          </Grid>
        </Grid>
      </Paper>

      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1.5, ml: 1 }}>
        ITEMS ({items.length})
      </Typography>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: 'action.hover' }}>
              <TableCell sx={{ fontWeight: 700 }}>Code</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Description</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>NCM/CFOP</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">Qty</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>UoM</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">Unit Price</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">Total</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((item, index) => (
              <TableRow key={index} hover>
                <TableCell sx={{ fontWeight: 600 }}>{item.materialCode}</TableCell>
                <TableCell>{item.originalDescription || '---'}</TableCell>
                <TableCell>
                  <Typography variant="caption" sx={{ display: 'block', fontFamily: 'monospace' }}>{item.ncm || '---'}</Typography>
                  <Typography variant="caption" color="text.secondary">{item.cfop || '---'}</Typography>
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 700 }}>{item.quantity}</TableCell>
                <TableCell>
                  <Chip label={item.unitOfMeasure} size="small" variant="outlined" sx={{ fontWeight: 700, fontSize: '0.65rem', height: 20 }} />
                </TableCell>
                <TableCell align="right">
                  {item.unitPrice !== undefined && item.unitPrice !== null
                    ? `R$ ${item.unitPrice.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
                    : '---'}
                </TableCell>
                <TableCell align="right">
                  {item.totalPrice !== undefined && item.totalPrice !== null
                    ? `R$ ${item.totalPrice.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
                    : '---'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
