import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  IconButton,
  Tooltip,
  ToggleButtonGroup,
  ToggleButton
} from '@mui/material';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import type { InventoryBalance } from '../types';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { BalanceDetailsModal } from './BalanceDetailsModal';
import { getStatusMapping } from '../../../shared/lib/utils/status-mapping';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';

type InventoryStocksPageProps = {
  tenantCode: string;
};

type FilterStatus = 'ALL' | 'Pending' | 'Available' | 'Blocked';

/**
 * Displays the current inventory balances with status filtering.
 * @param tenantCode - The active tenant identifier.
 * @remarks
 * This page serves as the primary view for tracking material stock and inbound status.
 * It uses a high-density table to display traceability information including Lot and Expiry.
 */
export function InventoryStocksPage({ tenantCode }: InventoryStocksPageProps) {
  const [balances, setBalances] = useState<InventoryBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<FilterStatus>('ALL');

  useEffect(() => {
    const fetchBalances = async () => {
      setLoading(true);
      setError(null);

      try {
        const queryParams = statusFilter !== 'ALL' ? `?status=${statusFilter}` : '';
        const data = await fetchJsonOrThrow<InventoryBalance[]>(
          `/api/inventory/balances${queryParams}`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Inventory request failed'
        );
        setBalances(data);
      } catch (requestError) {
        setError(requestError instanceof Error ? requestError.message : 'Inventory request failed.');
      } finally {
        setLoading(false);
      }
    };

    void fetchBalances();
  }, [tenantCode, statusFilter]);

  const handleFilterChange = (
    _event: React.MouseEvent<HTMLElement>,
    newStatus: FilterStatus | null
  ) => {
    if (newStatus !== null) {
      setStatusFilter(newStatus);
    }
  };

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <Box>
          <Typography variant="h1" sx={{ fontWeight: 900, mb: 0.5 }}>
            INVENTORY
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
            CURRENT STOCK AND PENDING BALANCES
          </Typography>
        </Box>

        <ToggleButtonGroup
          value={statusFilter}
          exclusive
          onChange={handleFilterChange}
          size="small"
          aria-label="status filter"
        >
          <ToggleButton value="ALL" sx={{ fontWeight: 700, px: 2 }}>ALL</ToggleButton>
          <ToggleButton value="Pending" sx={{ fontWeight: 700, px: 2 }}>PENDING</ToggleButton>
          <ToggleButton value="Available" sx={{ fontWeight: 700, px: 2 }}>AVAILABLE</ToggleButton>
          <ToggleButton value="Blocked" sx={{ fontWeight: 700, px: 2 }}>BLOCKED</ToggleButton>
        </ToggleButtonGroup>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress size={32} />
        </Box>
      ) : null}

      {error ? (
        <Alert severity="error" sx={{ my: 2 }}>
          {error}
        </Alert>
      ) : null}

      {!loading && !error && balances.length === 0 ? (
        <Box sx={{ p: 4, textAlign: 'center', bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }}>
          <Typography color="text.secondary">No balances found for the selected filter.</Typography>
        </Box>
      ) : null}

      {!loading && !error && balances.length > 0 ? (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 700 }}>Material</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Lot / Expiry</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Quantity</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>UoM</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Source / Supplier</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Created At</TableCell>
                <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {balances.map(balance => {
                const status = getStatusMapping(balance.status);
                return (
                  <TableRow key={balance.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <MaterialAvatar
                          materialCode={balance.materialCode}
                          description={balance.materialName}
                          imageUrl={balance.materialImageUrl}
                          size={32}
                        />
                        <Box>
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {balance.materialName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontWeight: 600 }}>
                            SKU: {balance.materialCode}
                            {balance.ncm && ` | NCM: ${balance.ncm}`}
                            {balance.gtin && ` | GTIN: ${balance.gtin}`}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>{balance.lotNumber || 'N/A'}</Typography>
                      {balance.expirationDate && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          EXP: {formatRelativeDate(balance.expirationDate, false)}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>{balance.quantity}</TableCell>
                    <TableCell>{balance.unitOfMeasure}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={status.label}
                        color={status.color as any}
                        variant="outlined"
                        sx={{ fontWeight: 700 }}
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{balance.supplierName || balance.sourceType}</Typography>
                      <Tooltip title="Click to copy full reference">
                        <Typography 
                          variant="caption" 
                          color="text.secondary" 
                          sx={{ display: 'block', fontFamily: 'monospace', fontSize: '0.65rem', cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => TechnicalIdFormatter.copyToClipboard(balance.sourceReference)}
                        >
                          {TechnicalIdFormatter.truncate(balance.sourceReference)}
                        </Typography>
                      </Tooltip>
                    </TableCell>
                    <TableCell>{formatRelativeDate(balance.createdAt)}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="View Full Ledger">
                        <IconButton 
                          size="small" 
                          color="info"
                          onClick={() => setSelectedBalanceId(balance.id)}
                        >
                          <InfoOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}

      <BalanceDetailsModal 
        balanceId={selectedBalanceId} 
        tenantCode={tenantCode} 
        onClose={() => setSelectedBalanceId(null)} 
      />
    </Box>
  );
}
