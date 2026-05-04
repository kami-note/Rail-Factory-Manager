import React, { useState } from 'react';
import { 
  Box, 
  Typography, 
  TextField, 
  Button, 
  Stack, 
  Grid, 
  Alert,
  CircularProgress
} from '@mui/material';
import type { Supplier } from './types';

type NewReceiptFormProps = {
  tenantCode: string;
  showTitle?: boolean;
};

export function NewReceiptForm({ tenantCode, showTitle = true }: NewReceiptFormProps) {
  const [supplierFiscalId, setSupplierFiscalId] = useState('12345678000100');
  const [supplierName, setSupplierName] = useState('Supplier Dev');
  const [receiptNumber, setReceiptNumber] = useState('RCPT-001');
  const [documentNumber, setDocumentNumber] = useState('DOC-001');
  const [materialCode, setMaterialCode] = useState('MAT-001');
  const [quantity, setQuantity] = useState('10');
  const [uom, setUom] = useState('UN');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setResult(null);
    setLoading(true);

    try {
      const supplierResponse = await fetch('/api/supply-chain/suppliers', {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'X-Tenant-Code': tenantCode
        },
        body: JSON.stringify({ fiscalId: supplierFiscalId, name: supplierName })
      });

      if (!supplierResponse.ok) {
        throw new Error(`Supplier creation failed: ${supplierResponse.status}`);
      }

      const supplier = (await supplierResponse.json()) as Supplier;

      const receiptResponse = await fetch('/api/supply-chain/receipts', {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'X-Tenant-Code': tenantCode
        },
        body: JSON.stringify({
          receiptNumber,
          supplierId: supplier.id,
          documentNumber,
          receiptDate: new Date().toISOString().slice(0, 10),
          items: [
            {
              materialCode,
              expectedQuantity: Number(quantity),
              unitOfMeasure: uom
            }
          ]
        })
      });

      if (!receiptResponse.ok) {
        throw new Error(`Receipt creation failed: ${receiptResponse.status}`);
      }

      const created = await receiptResponse.json();
      setResult(`Receipt created: ${created.id ?? created.receiptId}`);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unexpected error creating receipt.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit}>
      {showTitle && (
        <Typography variant="h6" sx={{ mb: 3, fontWeight: 700 }}>
          New receipt
        </Typography>
      )}
      
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Supplier fiscal ID"
            variant="outlined"
            size="small"
            value={supplierFiscalId}
            onChange={e => setSupplierFiscalId(e.target.value)}
            required
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Supplier name"
            variant="outlined"
            size="small"
            value={supplierName}
            onChange={e => setSupplierName(e.target.value)}
            required
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Receipt number"
            variant="outlined"
            size="small"
            value={receiptNumber}
            onChange={e => setReceiptNumber(e.target.value)}
            required
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Document number"
            variant="outlined"
            size="small"
            value={documentNumber}
            onChange={e => setDocumentNumber(e.target.value)}
            required
          />
        </Grid>
        <Grid size={12}>
          <Typography variant="subtitle2" sx={{ mt: 1, mb: 1, fontWeight: 700, color: 'text.secondary' }}>
            ITEM DETAILS
          </Typography>
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Material code"
            variant="outlined"
            size="small"
            value={materialCode}
            onChange={e => setMaterialCode(e.target.value)}
            required
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 3 }}>
          <TextField
            fullWidth
            label="Quantity"
            type="number"
            variant="outlined"
            size="small"
            value={quantity}
            onChange={e => setQuantity(e.target.value)}
            required
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 3 }}>
          <TextField
            fullWidth
            label="UoM"
            variant="outlined"
            size="small"
            value={uom}
            onChange={e => setUom(e.target.value)}
            required
          />
        </Grid>
      </Grid>

      <Stack spacing={2} sx={{ mt: 4 }}>
        <Button 
          type="submit" 
          variant="contained" 
          disabled={loading}
          startIcon={loading ? <CircularProgress size={20} /> : null}
        >
          {loading ? 'Creating...' : 'Create receipt'}
        </Button>
        
        {result && <Alert severity="success">{result}</Alert>}
        {error && <Alert severity="error">{error}</Alert>}
      </Stack>
    </Box>
  );
}
