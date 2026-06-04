import React, { useEffect, useState } from 'react';
import { 
  Box, 
  Typography, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  CircularProgress,
  Stack,
  alpha,
  useTheme,
  Avatar,
  Card,
  CardContent,
  Chip
} from '@mui/material';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { InlineError } from '../../../shared/components/common/InlineError';
import { formatRelativeDate, CurrencyFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';
import { 
  Building2, 
  Clock, 
  PackageOpen, 
  ShieldCheck, 
  FileText,
  AlertCircle
} from 'lucide-react';

type ReceiptDetailsModalProps = {
  receiptId: string | null;
  tenantCode: string;
  onClose: () => void;
};

type MaterialReceiptDetails = {
  id: string;
  receiptNumber: string;
  status: DisplayStatus;
  supplier?: {
    name: string;
    taxId: string;
  };
  issuedAt: string;
  audit: {
    createdAt: string;
    createdBy: string;
    conferenceStartedAt?: string;
    conferenceStartedBy?: string;
  };
  items: Array<{
    id: string;
    materialCode: string;
    productName: string;
    originalDescription?: string;
    expectedQuantity: number;
    countedQuantity?: number;
    unitOfMeasure: string;
    unitPrice?: number;
    lotNumber?: string;
    expirationDate?: string;
    imageUrl?: string;
  }>;
  timeline: Array<{
    status: DisplayStatus;
    occurredAt: string;
  }>;
};

/**
 * ReceiptDetailsModal orchestrates the fetching and displaying of an NF-e details.
 * Refactored using Leaf Node Pattern to ensure internal modularity.
 */
export function ReceiptDetailsModal({ receiptId, tenantCode, onClose }: ReceiptDetailsModalProps) {
  const [details, setDetails] = useState<MaterialReceiptDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!receiptId) return;

    const fetchDetails = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchJsonOrThrow<MaterialReceiptDetails>(
          `/api/supply-chain/receipts/${receiptId}`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Falha ao carregar detalhes do recebimento'
        );
        setDetails(data);
      } catch (err) {
        setError(toUiErrorMessage(err, 'Não foi possível carregar os detalhes do recebimento.'));
      } finally {
        setLoading(false);
      }
    };

    void fetchDetails();
  }, [receiptId, tenantCode]);

  return (
    <ResponsiveCenteredModal 
      open={!!receiptId} 
      title={details ? `DETALHES DA NOTA: ${details.receiptNumber}` : 'CARREGANDO...'} 
      onClose={onClose}
    >
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <InlineError message={error} />
      )}

      {details && !loading && (
        <Stack spacing={3}>
          <HeaderSection details={details} />
          <TimelineSection timeline={details.timeline} />
          <ItemsSection items={details.items} />
          <AuditSection audit={details.audit} systemId={details.id} />
        </Stack>
      )}
    </ResponsiveCenteredModal>
  );
}

// ----------------------------------------------------------------------
// LEAF NODE COMPONENTS (Local-First Rule)
// ----------------------------------------------------------------------

function HeaderSection({ details }: { details: MaterialReceiptDetails }) {
  const theme = useTheme();

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
      <Card variant="outlined" sx={{ borderRadius: 3, bgcolor: alpha(theme.palette.primary.main, 0.02) }}>
        <CardContent>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 1.5 }}>
            <Avatar sx={{ bgcolor: alpha(theme.palette.primary.main, 0.1), color: 'primary.main', width: 44, height: 44 }}>
              <Building2 size={22} />
            </Avatar>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800, lineHeight: 1 }}>FORNECEDOR</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.2 }}>
                {details.supplier?.name || 'Desconhecido'}
              </Typography>
            </Box>
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 1, ml: 0.5 }}>
            <FileText size={16} /> CNPJ: {details.supplier?.taxId || '-'}
          </Typography>
        </CardContent>
      </Card>

      <Card variant="outlined" sx={{ borderRadius: 3 }}>
        <CardContent>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', justifyContent: 'space-between', mb: 1.5 }}>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800, lineHeight: 1 }}>STATUS ATUAL</Typography>
              <Box sx={{ mt: 0.5 }}>
                <StatusChip status={details.status} />
              </Box>
            </Box>
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 1, ml: 0.5, mt: 1.5 }}>
            <Clock size={16} /> Emissão: {formatRelativeDate(details.issuedAt)}
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
}

function TimelineSection({ timeline }: { timeline: MaterialReceiptDetails['timeline'] }) {
  const theme = useTheme();

  return (
    <Card variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
      <Box sx={{ bgcolor: 'background.default', p: 1.5, px: 2, borderBottom: '1px solid', borderColor: 'divider', display: 'flex', alignItems: 'center', gap: 1 }}>
        <Clock size={18} color={theme.palette.text.secondary} />
        <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>LINHA DO TEMPO</Typography>
      </Box>
      <CardContent>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
          {timeline.map((event, idx) => {
            const statusColor = (theme.palette as any)[event.status.color]?.main || theme.palette.grey[400];
            return (
              <Box 
                key={idx} 
                sx={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  gap: 1.5,
                  p: 1.5,
                  borderRadius: 2,
                  bgcolor: alpha(statusColor, 0.05),
                  border: '1px solid',
                  borderColor: alpha(statusColor, 0.2),
                  minWidth: '200px'
                }}
              >
                <Box 
                  sx={{ 
                    width: 12, 
                    height: 12, 
                    borderRadius: '50%', 
                    bgcolor: statusColor,
                    boxShadow: `0 0 0 3px ${alpha(statusColor, 0.2)}`
                  }} 
                />
                <Box>
                  <Typography variant="body2" sx={{ fontWeight: 700, color: statusColor }}>
                    {event.status.label}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                    {formatRelativeDate(event.occurredAt)}
                  </Typography>
                </Box>
              </Box>
            );
          })}
        </Box>
      </CardContent>
    </Card>
  );
}

function ItemsSection({ items }: { items: MaterialReceiptDetails['items'] }) {
  const theme = useTheme();

  return (
    <Card variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
      <Box sx={{ bgcolor: 'background.default', p: 1.5, px: 2, borderBottom: '1px solid', borderColor: 'divider', display: 'flex', alignItems: 'center', gap: 1 }}>
        <PackageOpen size={18} color={theme.palette.text.secondary} />
        <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>CONFERÊNCIA DE ITENS</Typography>
        <Chip size="small" label={`${items.length} UN`} sx={{ ml: 'auto', fontWeight: 800, bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }} />
      </Box>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: 'background.default' }}>
              <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
              <TableCell align="center" sx={{ fontWeight: 800 }}>ESPERADO</TableCell>
              <TableCell align="center" sx={{ fontWeight: 800 }}>CONTADO</TableCell>
              <TableCell align="center" sx={{ fontWeight: 800 }}>DIFERENÇA</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>LOTE / VALIDADE</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((item) => {
              const diff = item.countedQuantity !== undefined ? item.countedQuantity - item.expectedQuantity : null;
              const hasDivergence = diff !== null && diff !== 0;
              
              return (
                <TableRow 
                  key={item.id} 
                  hover
                  sx={{
                    bgcolor: hasDivergence ? alpha(theme.palette.error.main, 0.02) : 'inherit'
                  }}
                >
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                      <MaterialAvatar 
                        materialCode={item.materialCode} 
                        imageUrl={item.imageUrl}
                        size={32} 
                      />
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {item.productName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" component="span" sx={{ display: 'block', fontWeight: 600, fontFamily: 'monospace' }}>
                          {item.materialCode}
                        </Typography>
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell align="center">
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{item.expectedQuantity}</Typography>
                    <Typography variant="caption" color="text.secondary">{item.unitOfMeasure}</Typography>
                  </TableCell>
                  <TableCell align="center">
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>{item.countedQuantity ?? '-'}</Typography>
                    <Typography variant="caption" color="text.secondary">{item.countedQuantity !== undefined ? item.unitOfMeasure : ''}</Typography>
                  </TableCell>
                  <TableCell align="center">
                    {diff !== null ? (
                      <Chip 
                        size="small" 
                        icon={hasDivergence ? <AlertCircle size={14} /> : undefined}
                        label={diff > 0 ? `+${diff}` : diff} 
                        color={diff === 0 ? 'success' : 'error'}
                        variant={diff === 0 ? 'outlined' : 'filled'}
                        sx={{ fontWeight: 800, minWidth: 60 }}
                      />
                    ) : (
                      '-'
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="caption" sx={{ display: 'block', fontWeight: 600 }}>{item.lotNumber || '-'}</Typography>
                    <Typography variant="caption" color="text.secondary">{item.expirationDate ? formatRelativeDate(item.expirationDate, false) : '-'}</Typography>
                    <Typography variant="caption" color="primary.main" sx={{ display: 'block', fontWeight: 700 }}>
                      {CurrencyFormatter.format(item.unitPrice)}
                    </Typography>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>
    </Card>
  );
}

function AuditSection({ audit, systemId }: { audit: MaterialReceiptDetails['audit'], systemId: string }) {
  const theme = useTheme();

  return (
    <Box sx={{ bgcolor: alpha(theme.palette.primary.main, 0.04), p: 2.5, borderRadius: 3, border: '1px solid', borderColor: alpha(theme.palette.primary.main, 0.1) }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2 }}>
        <ShieldCheck size={18} color={theme.palette.primary.main} />
        <Typography variant="subtitle2" color="primary.main" sx={{ fontWeight: 800 }}>TRILHA DE AUDITORIA DE SISTEMA</Typography>
      </Stack>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3 }}>
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>Registro Inicial</Typography>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{formatRelativeDate(audit.createdAt)}</Typography>
          <Typography variant="caption" sx={{ display: 'block', color: 'text.secondary' }}>Por: {audit.createdBy}</Typography>
        </Box>
        {audit.conferenceStartedAt && (
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>Início da Conferência</Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>{formatRelativeDate(audit.conferenceStartedAt)}</Typography>
            <Typography variant="caption" sx={{ display: 'block', color: 'text.secondary' }}>Por: {audit.conferenceStartedBy}</Typography>
          </Box>
        )}
      </Box>
      <Box sx={{ mt: 2, pt: 1.5, borderTop: '1px dashed', borderColor: 'divider' }}>
        <Typography variant="caption" sx={{ color: 'text.disabled', fontFamily: 'monospace' }}>
          System ID: {systemId}
        </Typography>
      </Box>
    </Box>
  );
}
