import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  TextField,
  Autocomplete,
  CircularProgress,
  Alert,
  Box,
  Stack,
  alpha,
  useTheme
} from '@mui/material';
import { GitMerge as MergeIcon, AlertTriangle, Search, Check } from 'lucide-react';
import { searchMaterials, mergeMaterials } from '../api/materials';
import { MaterialSearchResult } from '../types';

interface MergeMaterialModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess: (officialCode: string) => void;
  tenantCode: string;
  obsoleteMaterialCode: string;
  obsoleteMaterialName: string;
}

/**
 * Modal for unifying a duplicate material into an official material.
 * This process transfers balances and marks the current SKU as Obsolete.
 */
export function MergeMaterialModal({
  open,
  onClose,
  onSuccess,
  tenantCode,
  obsoleteMaterialCode,
  obsoleteMaterialName
}: MergeMaterialModalProps) {
  const theme = useTheme();
  const [loading, setLoading] = useState(false);
  const [searching, setSearching] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [options, setOptions] = useState<MaterialSearchResult[]>([]);
  const [selectedMaterial, setSelectedMaterial] = useState<MaterialSearchResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Search logic with debounce effect
  useEffect(() => {
    if (searchTerm.length < 2) {
      setOptions([]);
      return;
    }

    const delayDebounce = setTimeout(async () => {
      setSearching(true);
      try {
        const results = await searchMaterials(tenantCode, searchTerm);
        // Filter out the current material from results
        setOptions(results.filter(m => m.materialCode !== obsoleteMaterialCode));
      } catch (err) {
        console.error('Search failed', err);
      } finally {
        setSearching(false);
      }
    }, 400);

    return () => clearTimeout(delayDebounce);
  }, [searchTerm, tenantCode, obsoleteMaterialCode]);

  const handleMerge = async () => {
    if (!selectedMaterial) return;

    setLoading(true);
    setError(null);
    try {
      await mergeMaterials(tenantCode, obsoleteMaterialCode, selectedMaterial.materialCode);
      onSuccess(selectedMaterial.materialCode);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao processar unificação');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 800, display: 'flex', alignItems: 'center', gap: 1 }}>
        <MergeIcon size={24} color={theme.palette.primary.main} />
        Unificar Materiais (Merge)
      </DialogTitle>
      
      <DialogContent dividers>
        <Stack spacing={3}>
          <Box sx={{ p: 2, bgcolor: alpha(theme.palette.warning.main, 0.05), borderRadius: 2, border: 1, borderColor: 'warning.light' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, color: 'warning.dark', mb: 1, display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <AlertTriangle size={16} /> ATENÇÃO
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Você está prestes a converter o SKU <strong>{obsoleteMaterialCode}</strong> ({obsoleteMaterialName}) em um item obsoleto. 
              Todos os saldos físicos atuais serão transferidos para o material oficial selecionado.
            </Typography>
          </Box>

          <Box>
            <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1 }}>
              BUSCAR MATERIAL OFICIAL (ALVO)
            </Typography>
            <Autocomplete
              fullWidth
              options={options}
              getOptionLabel={(option) => `${option.materialCode} - ${option.officialName}`}
              loading={searching}
              onInputChange={(_, value) => setSearchTerm(value)}
              onChange={(_, value) => setSelectedMaterial(value)}
              renderInput={(params) => (
                <TextField
                  {...params}
                  placeholder="Digite código, nome ou GTIN..."
                  variant="outlined"
                />
              )}
              renderOption={(props, option) => {
                const { key, ...optionProps } = props as any;
                return (
                  <li key={option.materialCode} {...optionProps}>
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{option.materialCode} - {option.officialName}</Typography>
                      <Typography variant="caption" color="text.secondary">{option.description}</Typography>
                    </Box>
                  </li>
                );
              }}
            />
          </Box>

          {selectedMaterial && (
            <Alert severity="success" icon={<Check size={20} />} variant="outlined">
              O saldo será transferido para: <strong>{selectedMaterial.officialName}</strong> ({selectedMaterial.materialCode})
            </Alert>
          )}

          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ p: 2 }}>
        <Button onClick={onClose} disabled={loading}>Cancelar</Button>
        <Button 
          variant="contained" 
          color="primary" 
          onClick={handleMerge} 
          disabled={!selectedMaterial || loading}
          startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <MergeIcon size={18} />}
          sx={{ fontWeight: 700 }}
        >
          Confirmar Unificação
        </Button>
      </DialogActions>
    </Dialog>
  );
}
