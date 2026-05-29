import React, { useEffect, useState } from 'react';
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import { UserPlus, X } from 'lucide-react';
import { InlineError } from '../../../shared/components/common/InlineError';
import { createPerson } from '../api/hr';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { Person } from '../types';

const PERSON_TYPE_OPTIONS = [
  { value: 'Employee',   label: 'Colaborador' },
  { value: 'Driver',     label: 'Motorista' },
  { value: 'Contractor', label: 'Terceirizado' },
];

type Props = {
  open: boolean;
  tenantCode: string;
  onCreated: (person: Person) => void;
  onClose: () => void;
};

export function CreatePersonModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [name, setName] = useState('');
  const [documentNumber, setDocumentNumber] = useState('');
  const [type, setType] = useState('Employee');
  const [email, setEmail] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setName(''); setDocumentNumber(''); setType('Employee'); setEmail('');
      setError(null); setSaving(false);
    }
  }, [open]);

  const isValid = name.trim().length > 0 && documentNumber.trim().length > 0;

  const handleSubmit = async () => {
    if (!isValid) return;
    setSaving(true);
    setError(null);
    try {
      const person = await createPerson(tenantCode, {
        name: name.trim(),
        documentNumber: documentNumber.trim(),
        type,
        email: email.trim() || undefined,
      });
      onCreated(person);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível cadastrar a pessoa.'));
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        Nova Pessoa
        <IconButton size="small" onClick={handleClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <InlineError message={error} marginBottom={0} />}

          <TextField
            label="Nome completo"
            size="small"
            fullWidth
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="João da Silva"
            autoFocus
          />

          <Stack direction="row" spacing={2}>
            <TextField
              label="CPF / Documento"
              size="small"
              sx={{ flexGrow: 1 }}
              value={documentNumber}
              onChange={e => setDocumentNumber(e.target.value)}
              placeholder="000.000.000-00"
              slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }}
            />
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Tipo</InputLabel>
              <Select value={type} label="Tipo" onChange={e => setType(e.target.value)}>
                {PERSON_TYPE_OPTIONS.map(opt => (
                  <MenuItem key={opt.value} value={opt.value}>{opt.label}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>

          <TextField
            label="E-mail (opcional)"
            size="small"
            fullWidth
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="joao@exemplo.com"
            onKeyDown={e => { if (e.key === 'Enter' && isValid) void handleSubmit(); }}
          />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !isValid}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <UserPlus size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Cadastrar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
