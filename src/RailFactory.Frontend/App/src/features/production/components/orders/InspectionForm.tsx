import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { CheckCircle, XCircle } from 'lucide-react';
import { recordInspection } from '../../api/production';
import { Authorized } from '../../../auth';
import { useAuthSessionContext } from '../../../auth/context/AuthSessionContext';
import { toUiErrorMessage } from '../../../../shared/lib/http';

export function InspectionForm({ tenantCode, orderId, onRecorded }: {
  tenantCode: string;
  orderId: string;
  onRecorded: () => void;
}) {
  const { session } = useAuthSessionContext();
  const inspectedBy = session.authenticated
    ? (session.user?.name ?? session.user?.email ?? 'Usuário')
    : 'Usuário';

  const [result, setResult] = useState<'Passed' | 'Failed'>('Passed');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async () => {
    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await recordInspection(tenantCode, orderId, {
        result,
        inspectedBy,
        notes: notes.trim() || undefined,
      });
      setSuccess(true);
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

      {/* Resultado */}
      <Stack direction="row" spacing={1}>
        <Button
          fullWidth
          variant={result === 'Passed' ? 'contained' : 'outlined'}
          color="success"
          onClick={() => setResult('Passed')}
          startIcon={<CheckCircle size={16} />}
          sx={{ fontWeight: 800, py: 1.25 }}
        >
          Aprovado
        </Button>
        <Button
          fullWidth
          variant={result === 'Failed' ? 'contained' : 'outlined'}
          color="error"
          onClick={() => setResult('Failed')}
          startIcon={<XCircle size={16} />}
          sx={{ fontWeight: 800, py: 1.25 }}
        >
          Reprovado
        </Button>
      </Stack>

      {/* Observações */}
      <TextField
        label="Observações"
        placeholder={
          result === 'Failed'
            ? 'Descreva o defeito encontrado, localização, medição fora de especificação...'
            : 'Aspectos verificados, conformidade com especificação, número do lote...'
        }
        size="small"
        fullWidth
        multiline
        rows={4}
        value={notes}
        onChange={e => setNotes(e.target.value)}
      />

      {/* Inspecionado por — preenchido automaticamente */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
        <Typography variant="caption" color="text.secondary">Inspecionado por:</Typography>
        <Typography variant="caption" sx={{ fontWeight: 700 }}>{inspectedBy}</Typography>
      </Box>

      <Authorized permission="production.write">
        <Button
          variant="contained"
          color={result === 'Passed' ? 'success' : 'error'}
          fullWidth
          onClick={() => void handleSubmit()}
          disabled={saving}
          sx={{ fontWeight: 800 }}
        >
          {saving ? <CircularProgress size={18} color="inherit" /> : 'Registrar Inspeção'}
        </Button>
      </Authorized>
    </Stack>
  );
}
