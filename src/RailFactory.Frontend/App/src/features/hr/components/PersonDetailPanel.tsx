import React, { useEffect, useState, useRef } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Rating,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
  useTheme,
  alpha,
} from '@mui/material';
import { Clock, Star, Calendar, Trash2, Image as ImageIcon } from 'lucide-react';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { Authorized } from '../../auth';
import {
  logHours,
  listHourLogs,
  listSkills,
  addSkill,
  removeSkill,
  listShifts,
  createShift,
  deleteShift,
  uploadPersonImage,
} from '../api/hr';
import type { Person, HourLog, Skill, WorkShift } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

export function PersonDetailPanel({
  tenantCode,
  person,
  onUpdated,
}: {
  tenantCode: string;
  person: Person;
  onUpdated?: (updated: Person) => void;
}) {
  const [tab, setTab] = useState(0);
  const theme = useTheme();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const handleImageClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setUploading(true);
    setUploadError(null);
    try {
      const result = await uploadPersonImage(tenantCode, person.id, file);
      if (onUpdated) {
        onUpdated({ ...person, imageUrl: result.imageUrl });
      }
    } catch (err) {
      setUploadError(toUiErrorMessage(err, 'Falha ao enviar imagem.'));
    } finally {
      setUploading(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 2 }}>
        <input
          type="file"
          accept="image/png, image/jpeg, image/webp"
          style={{ display: 'none' }}
          ref={fileInputRef}
          onChange={handleFileChange}
          disabled={uploading}
        />
        
        <Box
          onClick={uploading ? undefined : handleImageClick}
          sx={{
            width: 72,
            height: 72,
            borderRadius: '50%',
            border: '2px dashed',
            borderColor: uploading ? 'primary.main' : 'divider',
            bgcolor: alpha(theme.palette.primary.main, 0.02),
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: uploading ? 'default' : 'pointer',
            overflow: 'hidden',
            position: 'relative',
            transition: 'all 0.2s',
            flexShrink: 0,
            '&:hover': {
              borderColor: 'primary.main',
              bgcolor: alpha(theme.palette.primary.main, 0.05),
            }
          }}
        >
          {uploading ? (
            <CircularProgress size={20} />
          ) : person.imageUrl ? (
            <Box sx={{ position: 'relative', width: '100%', height: '100%' }}>
              <Box
                component="img"
                src={person.imageUrl}
                sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
                alt={person.name}
              />
              <Box
                sx={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  width: '100%',
                  height: '100%',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  color: 'white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  opacity: 0,
                  transition: 'opacity 0.2s',
                  fontSize: '0.65rem',
                  fontWeight: 700,
                  textAlign: 'center',
                  p: 0.5,
                  '&:hover': {
                    opacity: 1
                  }
                }}
              >
                Alterar
              </Box>
            </Box>
          ) : (
            <>
              <ImageIcon size={20} color={theme.palette.text.secondary} />
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.65rem', fontWeight: 700, mt: 0.5 }}>
                Foto
              </Typography>
            </>
          )}
        </Box>

        <Box sx={{ minWidth: 0, flexGrow: 1 }}>
          <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>PESSOA</Typography>
          <Typography variant="h6" sx={{ fontWeight: 800, mt: -0.5, lineHeight: 1.2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {person.name}
          </Typography>
          <Stack direction="row" spacing={1} sx={{ mt: 0.5, flexWrap: 'wrap', gap: 0.5 }}>
            <StatusChip status={person.type} />
            <StatusChip status={person.status} />
          </Stack>
        </Box>
      </Stack>

      {uploadError && (
        <Alert severity="error" onClose={() => setUploadError(null)} sx={{ mb: 2, py: 0 }}>
          {uploadError}
        </Alert>
      )}

      <Tabs
        value={tab}
        onChange={(_, v) => setTab(v as number)}
        variant="fullWidth"
        sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}
      >
        <Tab label="Horas" icon={<Clock size={13} />} iconPosition="start" sx={{ minHeight: 40, fontSize: '0.75rem' }} />
        <Tab label="Competências" icon={<Star size={13} />} iconPosition="start" sx={{ minHeight: 40, fontSize: '0.75rem' }} />
        <Tab label="Turnos" icon={<Calendar size={13} />} iconPosition="start" sx={{ minHeight: 40, fontSize: '0.75rem' }} />
      </Tabs>

      {tab === 0 && <HoursTab tenantCode={tenantCode} person={person} />}
      {tab === 1 && <SkillsTab tenantCode={tenantCode} person={person} />}
      {tab === 2 && <ShiftsTab tenantCode={tenantCode} person={person} />}
    </Box>
  );
}

// ── Horas ────────────────────────────────────────────────────────────────────

function HoursTab({ tenantCode, person }: { tenantCode: string; person: Person }) {
  const [logs, setLogs] = useState<HourLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [hours, setHours] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setLogs(await listHourLogs(tenantCode, person.id));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar os apontamentos.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [person.id]);

  const handleLog = async () => {
    setSaving(true);
    setSaveError(null);
    try {
      const log = await logHours(tenantCode, person.id, { date, hoursWorked: Number(hours), description: description || undefined });
      setLogs(prev => [log, ...prev]);
      setHours('');
      setDescription('');
    } catch (err) {
      setSaveError(toUiErrorMessage(err, 'Não foi possível registrar as horas.'));
    } finally {
      setSaving(false);
    }
  };

  const totalHours = logs.reduce((s, l) => s + l.hoursWorked, 0);

  return (
    <Stack spacing={2}>
      <Authorized permission="hr.write">
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1.5 }}>
            REGISTRAR HORAS
          </Typography>
          {saveError && <Alert severity="error" onClose={() => setSaveError(null)} sx={{ mb: 1.5 }}>{saveError}</Alert>}
          <Stack spacing={1.5}>
            <TextField label="Data" type="date" size="small" fullWidth value={date} onChange={e => setDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField label="Horas trabalhadas" type="number" size="small" fullWidth value={hours} onChange={e => setHours(e.target.value)} slotProps={{ htmlInput: { min: 0.5, max: 24, step: 0.5 } }} />
            <TextField label="Descrição (opcional)" size="small" fullWidth value={description} onChange={e => setDescription(e.target.value)} />
            <Button variant="contained" size="small" onClick={() => void handleLog()} disabled={saving || !date || !hours} sx={{ fontWeight: 800 }}>
              {saving ? <CircularProgress size={16} color="inherit" /> : 'Registrar'}
            </Button>
          </Stack>
        </Paper>
      </Authorized>

      {loading ? (
        <Box sx={{ textAlign: 'center', py: 3 }}><CircularProgress size={24} /></Box>
      ) : error ? (
        <Alert severity="error">{error}</Alert>
      ) : logs.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
          Nenhum apontamento registrado.
        </Typography>
      ) : (
        <>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Typography variant="caption" color="text.secondary">
              Total: <strong>{totalHours.toLocaleString('pt-BR')}h</strong>
            </Typography>
          </Box>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ bgcolor: '#faf9f8' }}>
                <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem' }}>DATA</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: '0.7rem' }}>HORAS</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem' }}>DESCRIÇÃO</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.map(l => (
                <TableRow key={l.id} hover>
                  <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>{l.date}</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>{l.hoursWorked}h</TableCell>
                  <TableCell sx={{ color: 'text.secondary', fontSize: '0.8rem' }}>{l.description ?? '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      )}
    </Stack>
  );
}

// ── Competências ─────────────────────────────────────────────────────────────

function SkillsTab({ tenantCode, person }: { tenantCode: string; person: Person }) {
  const [skills, setSkills] = useState<Skill[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [skillName, setSkillName] = useState('');
  const [proficiency, setProficiency] = useState<number>(3);
  const [certifiedAt, setCertifiedAt] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setSkills(await listSkills(tenantCode, person.id));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar as competências.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [person.id]);

  const handleAdd = async () => {
    setSaving(true);
    setSaveError(null);
    try {
      const skill = await addSkill(tenantCode, person.id, {
        skillName: skillName.trim(),
        proficiencyLevel: proficiency,
        certifiedAt: certifiedAt || undefined,
        notes: notes.trim() || undefined,
      });
      setSkills(prev => [...prev, skill]);
      setSkillName('');
      setProficiency(3);
      setCertifiedAt('');
      setNotes('');
    } catch (err) {
      setSaveError(toUiErrorMessage(err, 'Não foi possível adicionar a competência.'));
    } finally {
      setSaving(false);
    }
  };

  const handleRemove = async (skillId: string) => {
    try {
      await removeSkill(tenantCode, person.id, skillId);
      setSkills(prev => prev.filter(s => s.id !== skillId));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível remover a competência.'));
    }
  };

  return (
    <Stack spacing={2}>
      <Authorized permission="hr.write">
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1.5 }}>
            ADICIONAR COMPETÊNCIA
          </Typography>
          {saveError && <Alert severity="error" onClose={() => setSaveError(null)} sx={{ mb: 1.5 }}>{saveError}</Alert>}
          <Stack spacing={1.5}>
            <TextField label="Nome da competência" size="small" fullWidth value={skillName} onChange={e => setSkillName(e.target.value)} />
            <Box>
              <Typography variant="caption" color="text.secondary">Nível de proficiência</Typography>
              <Rating value={proficiency} onChange={(_, v) => setProficiency(v ?? 1)} max={5} sx={{ display: 'block', mt: 0.5 }} />
            </Box>
            <TextField label="Certificado em (opcional)" type="date" size="small" fullWidth value={certifiedAt} onChange={e => setCertifiedAt(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField label="Observações (opcional)" size="small" fullWidth value={notes} onChange={e => setNotes(e.target.value)} />
            <Button variant="contained" size="small" onClick={() => void handleAdd()} disabled={saving || !skillName.trim()} sx={{ fontWeight: 800 }}>
              {saving ? <CircularProgress size={16} color="inherit" /> : 'Adicionar'}
            </Button>
          </Stack>
        </Paper>
      </Authorized>

      {loading ? (
        <Box sx={{ textAlign: 'center', py: 3 }}><CircularProgress size={24} /></Box>
      ) : error ? (
        <Alert severity="error">{error}</Alert>
      ) : skills.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
          Nenhuma competência cadastrada.
        </Typography>
      ) : (
        <Stack spacing={1}>
          {skills.map(s => (
            <Paper key={s.id} variant="outlined" sx={{ p: 1.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <Box sx={{ flexGrow: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>{s.skillName}</Typography>
                  <Rating value={s.proficiencyLevel} readOnly max={5} size="small" sx={{ mt: 0.25 }} />
                  {s.certifiedAt && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      Certificado em: {s.certifiedAt}
                    </Typography>
                  )}
                  {s.notes && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{s.notes}</Typography>
                  )}
                </Box>
                <Authorized permission="hr.write">
                  <Tooltip title="Remover">
                    <IconButton size="small" color="error" onClick={() => void handleRemove(s.id)}>
                      <Trash2 size={14} />
                    </IconButton>
                  </Tooltip>
                </Authorized>
              </Box>
            </Paper>
          ))}
        </Stack>
      )}
    </Stack>
  );
}

// ── Turnos ───────────────────────────────────────────────────────────────────

function ShiftsTab({ tenantCode, person }: { tenantCode: string; person: Person }) {
  const [shifts, setShifts] = useState<WorkShift[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [shiftDate, setShiftDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [startTime, setStartTime] = useState('08:00');
  const [endTime, setEndTime] = useState('17:00');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setShifts(await listShifts(tenantCode, person.id));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar os turnos.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [person.id]);

  const handleCreate = async () => {
    setSaving(true);
    setSaveError(null);
    try {
      const shift = await createShift(tenantCode, person.id, {
        shiftDate,
        startTime,
        endTime,
        notes: notes.trim() || undefined,
      });
      setShifts(prev => [shift, ...prev]);
      setNotes('');
    } catch (err) {
      setSaveError(toUiErrorMessage(err, 'Não foi possível criar o turno.'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (shiftId: string) => {
    try {
      await deleteShift(tenantCode, person.id, shiftId);
      setShifts(prev => prev.filter(s => s.id !== shiftId));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível excluir o turno.'));
    }
  };

  return (
    <Stack spacing={2}>
      <Authorized permission="hr.write">
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1.5 }}>
            CRIAR TURNO
          </Typography>
          {saveError && <Alert severity="error" onClose={() => setSaveError(null)} sx={{ mb: 1.5 }}>{saveError}</Alert>}
          <Stack spacing={1.5}>
            <TextField label="Data" type="date" size="small" fullWidth value={shiftDate} onChange={e => setShiftDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
            <Stack direction="row" spacing={1}>
              <TextField label="Início" type="time" size="small" sx={{ flexGrow: 1 }} value={startTime} onChange={e => setStartTime(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label="Fim" type="time" size="small" sx={{ flexGrow: 1 }} value={endTime} onChange={e => setEndTime(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
            </Stack>
            <TextField label="Observações (opcional)" size="small" fullWidth value={notes} onChange={e => setNotes(e.target.value)} />
            <Button variant="contained" size="small" onClick={() => void handleCreate()} disabled={saving || !shiftDate || !startTime || !endTime} sx={{ fontWeight: 800 }}>
              {saving ? <CircularProgress size={16} color="inherit" /> : 'Criar Turno'}
            </Button>
          </Stack>
        </Paper>
      </Authorized>

      {loading ? (
        <Box sx={{ textAlign: 'center', py: 3 }}><CircularProgress size={24} /></Box>
      ) : error ? (
        <Alert severity="error">{error}</Alert>
      ) : shifts.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
          Nenhum turno cadastrado.
        </Typography>
      ) : (
        <Stack spacing={1}>
          {shifts.map(s => (
            <Paper key={s.id} variant="outlined" sx={{ p: 1.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Box>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{s.shiftDate}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {s.startTime} → {s.endTime}
                  </Typography>
                  {s.notes && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{s.notes}</Typography>
                  )}
                </Box>
                <Authorized permission="hr.write">
                  <Tooltip title="Excluir">
                    <IconButton size="small" color="error" onClick={() => void handleDelete(s.id)}>
                      <Trash2 size={14} />
                    </IconButton>
                  </Tooltip>
                </Authorized>
              </Box>
            </Paper>
          ))}
        </Stack>
      )}
    </Stack>
  );
}
