import React from 'react';
import {
  Box,
  Button,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
  Card,
  CardContent,
  CardActions,
  Grid,
  Divider,
  useTheme,
  alpha,
} from '@mui/material';
import { 
  Info as InfoIcon,
  Calendar as CalendarIcon,
  Tag as TagIcon,
  History as HistoryIcon,
  Package as PackageIcon,
  Eye as EyeIcon,
} from 'lucide-react';
import type { InventoryBalance } from '../types';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';

const formatDate = (dateIso: string, includeTime = true) => {
  if (!dateIso) return '-';
  return new Date(dateIso).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    ...(includeTime ? { hour: '2-digit', minute: '2-digit' } : {}),
  });
};

export function InventoryDesktopTable({ balances, onNavigate, onDetails }: {
  balances: InventoryBalance[];
  onNavigate: (code: string) => void;
  onDetails: (id: string) => void;
}) {
  return (
    <TableContainer component={Paper} variant="outlined">
      <Table stickyHeader size="small">
        <TableHead>
          <TableRow>
            <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>LOTE / VALIDADE</TableCell>
            <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>UNIDADE</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>ORIGEM</TableCell>
            <TableCell sx={{ fontWeight: 800 }}>CRIADO EM</TableCell>
            <TableCell align="right" sx={{ fontWeight: 800 }}>AÇÕES</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {balances.map(b => (
            <TableRow key={b.id} hover>
              <TableCell>
                <Box
                  sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }}
                  onClick={() => onNavigate(b.materialCode)}
                >
                  <MaterialAvatar materialCode={b.materialCode} size={32} description={b.materialName} imageUrl={b.materialImageUrl} />
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main', '&:hover': { textDecoration: 'underline' } }}>
                      {b.materialName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                      {b.materialCode}
                    </Typography>
                  </Box>
                </Box>
              </TableCell>
              <TableCell>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>{b.lotNumber || 'N/A'}</Typography>
                {b.expirationDate && (
                  <Typography variant="caption" color="text.secondary">
                    {new Date(b.expirationDate).toLocaleDateString('pt-BR')}
                  </Typography>
                )}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 800 }}>{b.quantity}</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>{b.unitOfMeasure}</TableCell>
              <TableCell><StatusChip status={b.status} /></TableCell>
              <TableCell>
                <StatusChip status={b.sourceType} label={b.supplierName || b.sourceType.label} />
                <Tooltip title="Clique para copiar referência">
                  <Typography
                    variant="caption"
                    color="text.disabled"
                    sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', mt: 0.5 }}
                    onClick={() => TechnicalIdFormatter.copyToClipboard(b.sourceReference)}
                  >
                    {TechnicalIdFormatter.truncate(b.sourceReference)}
                  </Typography>
                </Tooltip>
              </TableCell>
              <TableCell>{formatDate(b.createdAt)}</TableCell>
              <TableCell align="right">
                {b.id === '00000000-0000-0000-0000-000000000000' ? (
                  <Tooltip title="Item sem movimentações no estoque">
                    <span>
                      <IconButton size="small" disabled>
                        <InfoIcon size={16} />
                      </IconButton>
                    </span>
                  </Tooltip>
                ) : (
                  <Tooltip title="Ver detalhes e histórico">
                    <IconButton size="small" onClick={() => onDetails(b.id)}>
                      <InfoIcon size={16} />
                    </IconButton>
                  </Tooltip>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function InventoryMobileList({ balances, onNavigate, onDetails }: {
  balances: InventoryBalance[];
  onNavigate: (code: string) => void;
  onDetails: (id: string) => void;
}) {
  return (
    <Stack spacing={2}>
      {balances.map(b => (
        <Paper key={b.id} variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
          <Stack spacing={2}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <Box
                sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }}
                onClick={() => onNavigate(b.materialCode)}
              >
                <MaterialAvatar materialCode={b.materialCode} size={40} description={b.materialName} imageUrl={b.materialImageUrl} />
                <Box>
                  <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>{b.materialName}</Typography>
                  <Typography variant="caption" color="text.secondary">{b.materialCode}</Typography>
                </Box>
              </Box>
              <StatusChip status={b.status} />
            </Box>
            <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
              <Typography variant="caption" color="text.secondary">Qtd: <strong>{b.quantity}</strong> {b.unitOfMeasure}</Typography>
              <Typography variant="caption" color="text.secondary">Lote: <strong>{b.lotNumber || 'N/A'}</strong></Typography>
              <Typography variant="caption" color="text.secondary">Origem: <StatusChip status={b.sourceType} label={b.supplierName || b.sourceType.label} /></Typography>
              <Typography variant="caption" color="text.secondary">Criado: <strong>{formatDate(b.createdAt)}</strong></Typography>
            </Box>
            <Button
              size="small"
              variant="outlined"
              startIcon={<InfoIcon size={14} />}
              onClick={() => onDetails(b.id)}
              disabled={b.id === '00000000-0000-0000-0000-000000000000'}
            >
              {b.id === '00000000-0000-0000-0000-000000000000' ? 'Sem Movimentações' : 'Ver Histórico Completo'}
            </Button>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}

export function InventoryBalanceCardList({ balances, onNavigate, onDetails }: {
  balances: InventoryBalance[];
  onNavigate: (code: string) => void;
  onDetails: (id: string) => void;
}) {
  const theme = useTheme();

  const getHashColor = (str: string) => {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash % 360);
    return `hsl(${hue}, 45%, 40%)`;
  };

  return (
    <Grid container spacing={3}>
      {balances.map(b => {
        const hashColor = getHashColor(b.materialCode);
        const formattedDate = formatDate(b.createdAt);

        return (
          <Grid size={{ xs: 12, sm: 6, md: 4, lg: 3 }} key={b.id}>
            <Card
              variant="outlined"
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                borderRadius: 2,
                overflow: 'hidden',
                bgcolor: 'background.paper',
                transition: 'transform 0.2s, box-shadow 0.2s, border-color 0.2s',
                position: 'relative',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: '0 8px 24px rgba(0, 0, 0, 0.08)',
                  borderColor: 'primary.main',
                },
              }}
            >
              {/* Header Image / Pattern Area */}
              <Box
                sx={{
                  height: 140,
                  position: 'relative',
                  overflow: 'hidden',
                  cursor: 'pointer',
                  bgcolor: alpha(hashColor, 0.1),
                }}
                onClick={() => onNavigate(b.materialCode)}
              >
                {b.materialImageUrl ? (
                  <Box
                    component="img"
                    src={b.materialImageUrl}
                    alt={b.materialName}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'cover',
                      transition: 'transform 0.3s',
                      '&:hover': {
                        transform: 'scale(1.05)',
                      },
                    }}
                  />
                ) : (
                  <Box
                    sx={{
                      width: '100%',
                      height: '100%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      background: `linear-gradient(135deg, ${hashColor} 0%, ${alpha(hashColor, 0.6)} 100%)`,
                    }}
                  >
                    <PackageIcon size={48} color="white" style={{ opacity: 0.9 }} />
                  </Box>
                )}

                {/* Material Code Badge */}
                <Box
                  sx={{
                    position: 'absolute',
                    top: 12,
                    left: 12,
                    bgcolor: 'rgba(32, 31, 30, 0.8)',
                    backdropFilter: 'blur(4px)',
                    color: 'white',
                    px: 1,
                    py: 0.5,
                    borderRadius: 0.5,
                    fontFamily: 'monospace',
                    fontSize: '0.7rem',
                    fontWeight: 700,
                  }}
                >
                  {b.materialCode}
                </Box>

                {/* Status Badge */}
                <Box sx={{ position: 'absolute', top: 12, right: 12 }}>
                  <StatusChip status={b.status} />
                </Box>
              </Box>

              {/* Card Body */}
              <CardContent sx={{ flexGrow: 1, p: 2, pb: 1 }}>
                {/* Material Name */}
                <Tooltip title={b.materialName} placement="top" arrow>
                  <Typography
                    variant="subtitle2"
                    onClick={() => onNavigate(b.materialCode)}
                    sx={{
                      fontWeight: 850,
                      color: 'text.primary',
                      cursor: 'pointer',
                      mb: 1.5,
                      minHeight: 40,
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      lineHeight: 1.3,
                      '&:hover': {
                        color: 'primary.main',
                        textDecoration: 'underline',
                      },
                    }}
                  >
                    {b.materialName}
                  </Typography>
                </Tooltip>

                {/* Balance highlight section */}
                <Box
                  sx={{
                    bgcolor: alpha(theme.palette.primary.main, 0.03),
                    borderLeft: `3px solid ${theme.palette.primary.main}`,
                    p: 1.5,
                    borderRadius: '0 4px 4px 0',
                    mb: 2,
                  }}
                >
                  <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 0.5 }}>
                    SALDO DISPONÍVEL
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 900, color: b.quantity > 0 ? 'success.main' : 'text.disabled', display: 'flex', alignItems: 'baseline', gap: 0.5 }}>
                    {b.quantity.toLocaleString('pt-BR')}
                    <Typography component="span" variant="body2" sx={{ fontWeight: 700, color: 'text.secondary' }}>
                      {b.unitOfMeasure}
                    </Typography>
                  </Typography>
                </Box>

                {/* Details list */}
                <Stack spacing={1} sx={{ mb: 1 }}>
                  {/* Lote */}
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      <TagIcon size={14} style={{ color: theme.palette.text.secondary }} />
                      <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary' }}>LOTE</Typography>
                    </Stack>
                    <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.primary', fontFamily: 'monospace' }}>
                      {b.lotNumber || 'N/A'}
                    </Typography>
                  </Box>

                  {/* Validade */}
                  {b.expirationDate && (
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <CalendarIcon size={14} style={{ color: theme.palette.text.secondary }} />
                        <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary' }}>VALIDADE</Typography>
                      </Stack>
                      <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.primary' }}>
                        {new Date(b.expirationDate).toLocaleDateString('pt-BR')}
                      </Typography>
                    </Box>
                  )}

                  {/* Origem */}
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary' }}>ORIGEM</Typography>
                    <StatusChip status={b.sourceType} label={b.supplierName || b.sourceType.label} />
                  </Box>

                  {/* Data Criado */}
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary' }}>CRIADO EM</Typography>
                    <Typography variant="caption" sx={{ fontWeight: 750, color: 'text.secondary' }}>
                      {formattedDate}
                    </Typography>
                  </Box>
                </Stack>
              </CardContent>

              <Divider sx={{ opacity: 0.5 }} />

              {/* Card Footer Actions */}
              <CardActions sx={{ justifyContent: 'space-between', px: 2, py: 1.5 }}>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<EyeIcon size={14} />}
                  onClick={() => onNavigate(b.materialCode)}
                  sx={{ fontWeight: 800, borderRadius: 1 }}
                >
                  Ver Ficha
                </Button>
                {b.id === '00000000-0000-0000-0000-000000000000' ? (
                  <Tooltip title="Item sem movimentações no estoque">
                    <span>
                      <Button
                        size="small"
                        variant="text"
                        color="secondary"
                        startIcon={<HistoryIcon size={14} />}
                        disabled
                        sx={{ fontWeight: 800 }}
                      >
                        Sem Histórico
                      </Button>
                    </span>
                  </Tooltip>
                ) : (
                  <Button
                    size="small"
                    variant="text"
                    color="secondary"
                    startIcon={<HistoryIcon size={14} />}
                    onClick={() => onDetails(b.id)}
                    sx={{ fontWeight: 800 }}
                  >
                    Histórico
                  </Button>
                )}
              </CardActions>
            </Card>
          </Grid>
        );
      })}
    </Grid>
  );
}
