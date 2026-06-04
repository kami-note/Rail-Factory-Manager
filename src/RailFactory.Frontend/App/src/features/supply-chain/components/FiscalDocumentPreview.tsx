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
  Chip,
  alpha,
  useTheme
} from '@mui/material';
import { Building2, FileText, Calendar, Hash } from 'lucide-react';
import { CurrencyFormatter } from '../../../shared/lib/utils/formatters';

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
 */
export function FiscalDocumentPreview({ data }: FiscalDocumentPreviewProps) {
  const theme = useTheme();
  if (!data) return null;

  const items = data.items || [];

  return (
    <Box sx={{ mt: 2 }}>
      <Typography variant="overline" sx={{ fontWeight: 800, mb: 2, color: 'primary.main', display: 'flex', alignItems: 'center', gap: 1 }}>
        <FileText size={16} /> RESUMO DO DOCUMENTO FISCAL
      </Typography>

      <Paper variant="outlined" sx={{ p: 3, bgcolor: alpha(theme.palette.primary.main, 0.01), mb: 3, borderRadius: 2 }}>
        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 6 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Building2 size={20} style={{ marginTop: 2, opacity: 0.7, color: theme.palette.primary.main }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
                  Fornecedor
                </Typography>
                <Typography variant="body1" sx={{ fontWeight: 800 }}>
                  {data.supplierName}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                  CNPJ: {data.supplierFiscalId}
                </Typography>
              </Box>
            </Box>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Calendar size={20} style={{ marginTop: 2, opacity: 0.7, color: theme.palette.primary.main }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
                  Data de Emissão
                </Typography>
                <Typography variant="body1" sx={{ fontWeight: 800 }}>
                  {data.receiptDate ? new Date(data.receiptDate).toLocaleDateString('pt-BR') : 'N/A'}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                  Nº Doc: {data.documentNumber}
                </Typography>
                {data.operationNature && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                    Natureza: {data.operationNature}
                  </Typography>
                )}
              </Box>
            </Box>
          </Grid>
          <Grid size={{ xs: 12 }}>
            <Divider sx={{ my: 1, borderStyle: 'dashed' }} />
          </Grid>
          <Grid size={{ xs: 12, md: 8 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Hash size={20} style={{ marginTop: 2, opacity: 0.7, color: theme.palette.primary.main }} />
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
                  Chave de Acesso NF-e
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600, fontFamily: 'monospace', letterSpacing: 1, wordBreak: 'break-all' }}>
                  {data.accessKey || 'N/A'}
                </Typography>
              </Box>
            </Box>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }} sx={{ textAlign: { md: 'right' } }}>
            <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
              Valor Total da Nota
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 900, color: 'success.main' }}>
              {data.totalValue !== undefined && data.totalValue !== null
                ? CurrencyFormatter.format(data.totalValue)
                : 'N/A'}
            </Typography>
          </Grid>
        </Grid>
      </Paper>

      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1.5, ml: 1 }}>
        ITENS DA NOTA ({items.length})
      </Typography>
      <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 2 }}>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: alpha(theme.palette.primary.main, 0.03) }}>
              <TableCell sx={{ fontWeight: 800 }}>CÓDIGO</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>DESCRIÇÃO</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>NCM / CFOP</TableCell>
              <TableCell sx={{ fontWeight: 800 }} align="right">QTD</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>UN</TableCell>
              <TableCell sx={{ fontWeight: 800 }} align="right">VALOR UNIT.</TableCell>
              <TableCell sx={{ fontWeight: 800 }} align="right">TOTAL</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((item, index) => (
              <TableRow key={index} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{item.materialCode}</TableCell>
                <TableCell sx={{ fontWeight: 500 }}>{item.originalDescription || '---'}</TableCell>
                <TableCell>
                  <Typography variant="caption" sx={{ display: 'block', fontWeight: 600, fontFamily: 'monospace' }}>{item.ncm || '---'}</Typography>
                  <Typography variant="caption" color="text.secondary">CFOP: {item.cfop || '---'}</Typography>
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 800 }}>{item.quantity}</TableCell>
                <TableCell>
                  <Chip label={item.unitOfMeasure} size="small" variant="outlined" sx={{ fontWeight: 800, fontSize: '0.65rem', height: 20 }} />
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 600 }}>
                  {item.unitPrice !== undefined && item.unitPrice !== null
                    ? CurrencyFormatter.format(item.unitPrice)
                    : '---'}
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 800, color: 'success.main' }}>
                  {item.totalPrice !== undefined && item.totalPrice !== null
                    ? CurrencyFormatter.format(item.totalPrice)
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
