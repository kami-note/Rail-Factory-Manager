import React, { useEffect, useState } from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Paper, 
  TextField, 
  CircularProgress,
  Alert,
  Stack
} from '@mui/material';
import { ChevronLeft, Save, ClipboardCheck } from 'lucide-react';
import type { ConferenceItem } from '../types';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';

type ConferenceWorkspaceProps = {
  /** The identifier for the material receipt to be conferred. */
  receiptId: string;
  /** The active tenant identifier. */
  tenantCode: string;
  /** Callback to close the workspace. */
  onClose: () => void;
  /** Callback after successful conference submission. */
  onSuccess?: () => void;
};

type CountedResult = {
  itemId: string;
  countedQuantity: number;
  confirmedLotNumber: string;
  confirmedExpirationDate: string;
};

/**
 * Renders the Blind Conference workspace for physical material counting.
 * @param props - Component properties.
 * @remarks
 * Architectural Invariant: This component implements the "Blind Conference" rule (RN-05).
 * It receives material codes but DOES NOT display expected quantities from the fiscal document,
 * forcing the operator to perform an unbiased physical count.
 */
export function ConferenceWorkspace({ receiptId, tenantCode, onClose, onSuccess }: ConferenceWorkspaceProps) {
  const [items, setItems] = useState<ConferenceItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [counts, setCounts] = useState<Record<string, CountedResult>>({});

  useEffect(() => {
    const fetchItems = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchJsonOrThrow<ConferenceItem[]>(
          `/api/supply-chain/receipts/${receiptId}/conference/items`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Failed to load conference items'
        );

        setItems(data);
        const initialCounts: Record<string, CountedResult> = {};
        data.forEach(item => {
          initialCounts[item.id] = {
            itemId: item.id,
            countedQuantity: 0,
            confirmedLotNumber: '',
            confirmedExpirationDate: ''
          };
        });
        setCounts(initialCounts);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    void fetchItems();
  }, [receiptId, tenantCode]);

  const handleInputChange = (itemId: string, field: keyof CountedResult, value: string | number) => {
    setCounts(prev => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        [field]: value
      }
    }));
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const payload = {
        results: Object.values(counts).map(c => ({
          itemId: c.itemId,
          countedQuantity: Number(c.countedQuantity),
          confirmedLotNumber: c.confirmedLotNumber || null,
          confirmedExpirationDate: c.confirmedExpirationDate ? new Date(c.confirmedExpirationDate).toISOString() : null
        }))
      };

      await fetchJsonOrThrow(
        `/api/supply-chain/receipts/${receiptId}/conference/close`,
        {
          method: 'POST',
          headers: {
            ...buildTenantHeaders(tenantCode),
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(payload),
          credentials: 'include'
        },
        'Failed to close conference'
      );

      if (onSuccess) {
        onSuccess();
      } else {
        onClose();
      }
    } catch (err) {
      console.error(err);
      alert('Error saving conference.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Box sx={{ p: 4, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Box>
      <ModuleHeader 
        label="BLIND CONFERENCE" 
        icon={<ClipboardCheck size={20} />}
        action={
          <Button 
            variant="outlined" 
            size="small" 
            startIcon={<ChevronLeft size={16} />} 
            onClick={onClose}
          >
            Back to List
          </Button>
        }
      />

      <Alert severity="info" sx={{ mt: 3, mb: 3 }}>
        Enter the actual quantities, lot numbers, and expiration dates as counted in the physical material.
      </Alert>

      <TableContainer component={Paper} variant="outlined">
        <Table stickyHeader size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: 'background.default' }}>
              <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>COUNTED QTY</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>UOM</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>LOT NUMBER</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>EXPIRATION DATE</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map(item => (
              <TableRow key={item.id} hover>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    <MaterialAvatar 
                      materialCode={item.materialCode} 
                      description={item.originalDescription} 
                      imageUrl={item.imageUrl}
                      size={32} 
                    />
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.primary' }}>
                        {item.originalDescription || 'No description'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontWeight: 600, fontFamily: 'monospace' }}>
                        {item.materialCode}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>
                <TableCell>
                  <TextField
                    type="number"
                    size="small"
                    value={counts[item.id]?.countedQuantity ?? 0}
                    onChange={(e) => handleInputChange(item.id, 'countedQuantity', e.target.value)}
                    sx={{ width: 100 }}
                  />
                </TableCell>
                <TableCell sx={{ fontWeight: 700 }}>{item.unitOfMeasure}</TableCell>
                <TableCell>
                  <TextField
                    size="small"
                    value={counts[item.id]?.confirmedLotNumber ?? ''}
                    onChange={(e) => handleInputChange(item.id, 'confirmedLotNumber', e.target.value)}
                    placeholder="Lot #"
                    fullWidth
                  />
                </TableCell>
                <TableCell>
                  <TextField
                    type="date"
                    size="small"
                    value={counts[item.id]?.confirmedExpirationDate ?? ''}
                    onChange={(e) => handleInputChange(item.id, 'confirmedExpirationDate', e.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                    fullWidth
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Stack direction="row" spacing={2} sx={{ mt: 4, justifyContent: 'flex-end' }}>
        <Button variant="outlined" onClick={onClose} disabled={saving}>
          CANCEL
        </Button>
        <Button 
          variant="contained" 
          startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <Save size={18} />}
          onClick={handleSave}
          disabled={saving}
          sx={{ px: 4, fontWeight: 800 }}
        >
          {saving ? 'SAVING...' : 'FINISH CONFERENCE'}
        </Button>
      </Stack>
    </Box>
  );
}
