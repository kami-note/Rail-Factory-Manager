import React, { useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Stack,
  TextField,
} from '@mui/material';
import { CheckCircle, XCircle } from 'lucide-react';
import { recordInspection } from '../../api/production';
import { Authorized } from '../../../auth';
import { toUiErrorMessage } from '../../../../shared/lib/http';

export function InspectionForm({ tenantCode, orderId, onRecorded }: {
  tenantCode: string;
  orderId: string;
  onRecorded: () => void;
}) {
  const [result, setResult] = useState<'Passed' | 'Failed'>('Passed');
  const [inspectedBy, setInspectedBy] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async () => {
    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await recordInspection(tenantCode, orderId, { result, inspectedBy: inspectedBy.trim(), notes: notes.trim() || undefined });
      setSuccess(true);
      setInspectedBy('');
      setNotes('');
      onRecorded();
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível registrar a inspeção.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Stack spacing={2}>
      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)}>Inspeção registrada.</Alert>}
      <Stack direction="row" spacing={1}>
        <Button fullWidth variant={result === 'Passed' ? 'contained' : 'outlined'} color="success" onClick={() => setResult('Passed')} startIcon={<CheckCircle size={15} />} sx={{ fontWeight: 800 }}>Aprovado</Button>
        <Button fullWidth variant={result === 'Failed' ? 'contained' : 'outlined'} color="error" onClick={() => setResult('Failed')} startIcon={<XCircle size={15} />} sx={{ fontWeight: 800 }}>Reprovado</Button>
      </Stack>
      <TextField label="Inspecionado por" size="small" fullWidth value={inspectedBy} onChange={e => setInspectedBy(e.target.value)} />
      <TextField label="Observações (opcional)" size="small" fullWidth multiline rows={2} value={notes} onChange={e => setNotes(e.target.value)} />
      <Authorized permission="production.write">
        <Button variant="contained" color={result === 'Passed' ? 'success' : 'error'} fullWidth onClick={() => void handleSubmit()} disabled={saving || !inspectedBy.trim()} sx={{ fontWeight: 800 }}>
          {saving ? <CircularProgress size={18} color="inherit" /> : 'Registrar Inspeção'}
        </Button>
      </Authorized>
    </Stack>
  );
}
