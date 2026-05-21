import React, { useState } from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Stack, 
  Alert, 
  CircularProgress,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Paper,
  Divider,
  alpha,
  useTheme
} from '@mui/material';
import { FileText, UploadCloud, X, CheckCircle, AlertCircle, Lock } from 'lucide-react';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { InlineError } from '../../../shared/components/common/InlineError';
import { FiscalDocumentPreview, ParsedReceiptDocument } from './FiscalDocumentPreview';
import { Authorized } from '../../auth';

type ImportXmlFormProps = {
  tenantCode: string;
  showTitle?: boolean;
};

type SelectedXmlFile = {
  file: File;
};

type BatchError = {
  fileName: string;
  message: string;
};

/**
 * Multi-file upload form for importing NF-e XML files.
 * @param props - Component properties.
 */
export function ImportXmlForm({ tenantCode, showTitle = true }: ImportXmlFormProps) {
  const theme = useTheme();
  const [files, setFiles] = useState<SelectedXmlFile[]>([]);
  const [loading, setLoading] = useState(false);
  const [previewData, setPreviewData] = useState<ParsedReceiptDocument | null>(null);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [batchErrors, setBatchErrors] = useState<BatchError[]>([]);

  const getPreview = async (file: File) => {
    setLoading(true);
    setError(null);
    setBatchErrors([]);
    const formData = new FormData();
    formData.append('file', file);

    try {
      const data = await fetchJsonOrThrow<ParsedReceiptDocument>(
        '/api/supply-chain/receipts/import/xml/preview',
        {
          method: 'POST',
          credentials: 'include',
          headers: buildTenantHeaders(tenantCode),
          body: formData
        },
        'Falha ao analisar XML para pré-visualização'
      );
      setPreviewData(data);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível pré-visualizar o XML.'));
    } finally {
      setLoading(false);
    }
  };

  const confirmImport = async () => {
    if (files.length === 0) return;
    setLoading(true);
    setError(null);
    setBatchErrors([]);

    try {
      if (files.length === 1) {
        const formData = new FormData();
        formData.append('file', files[0].file);

        const data = await fetchJsonOrThrow<{ receiptId?: string; id?: string }>(
          '/api/supply-chain/receipts/import/xml',
          {
            method: 'POST',
            credentials: 'include',
            headers: buildTenantHeaders(tenantCode),
            body: formData
          },
          'Falha na importação do XML'
        );
        setResult(`XML importado com sucesso. ID do Recebimento: ${data.receiptId ?? data.id}`);
        setPreviewData(null);
        setFiles([]);
      } else {
        const formData = new FormData();
        for (const f of files) {
          formData.append('files', f.file);
        }

        const data = await fetchJsonOrThrow<{ imported?: Array<{ receiptId: string }>; errors?: BatchError[] }>(
          '/api/supply-chain/receipts/import/xml/batch',
          {
            method: 'POST',
            credentials: 'include',
            headers: buildTenantHeaders(tenantCode),
            body: formData
          },
          'Importação em lote falhou'
        );

        if (data.errors?.length) {
          setBatchErrors(data.errors);
          throw new Error('Importação em lote falhou com erros de validação.');
        }
        setResult(`${data.imported?.length ?? 0} arquivos importados com sucesso.`);
        setFiles([]);
      }
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível importar o XML.'));
    } finally {
      setLoading(false);
    }
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files ?? []);
    if (selectedFiles.length === 0) return;
    
    setError(null);
    setResult(null);
    setPreviewData(null);
    setBatchErrors([]);
    setFiles(selectedFiles.map(file => ({ file })));

    if (selectedFiles.length === 1) {
      getPreview(selectedFiles[0]);
    }
  };

  const removeFiles = () => {
    setFiles([]);
    setResult(null);
    setError(null);
    setPreviewData(null);
    setBatchErrors([]);
  };

  return (
    <Box>
      {showTitle && (
        <Typography variant="h6" sx={{ mb: 3, fontWeight: 800 }}>
          Importar XML de Recebimento
        </Typography>
      )}

      <Stack spacing={3}>
        {!previewData && !result && (
          <Authorized 
            permission="supplychain.write"
            fallback={
              <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', bgcolor: alpha(theme.palette.error.main, 0.02), borderRadius: 2 }}>
                <Lock size={48} color={theme.palette.error.main} style={{ marginBottom: 16 }} />
                <Typography variant="body1" sx={{ fontWeight: 800 }}>Acesso Restrito</Typography>
                <Typography variant="body2" color="text.secondary">Você não tem permissão para importar novos documentos fiscais.</Typography>
              </Paper>
            }
          >
            <Paper
              variant="outlined"
              sx={{
                p: 4,
                textAlign: 'center',
                cursor: 'pointer',
                borderStyle: 'dashed',
                borderWidth: 2,
                bgcolor: alpha(theme.palette.primary.main, 0.02),
                '&:hover': {
                  borderColor: 'primary.main',
                  bgcolor: alpha(theme.palette.primary.main, 0.05)
                },
                position: 'relative',
                borderRadius: 2
              }}
              component="label"
            >
              <input
                type="file"
                accept=".xml,text/xml,application/xml"
                multiple
                hidden
                onChange={handleFileChange}
                onClick={(e) => ((e.target as HTMLInputElement).value = '')}
              />
              <UploadCloud size={48} color={theme.palette.primary.main} style={{ marginBottom: 16 }} />
              <Typography variant="body1" sx={{ fontWeight: 700 }}>
                Clique ou arraste arquivos XML aqui
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Suporta Nota Fiscal avulsa ou importação em lote
              </Typography>
            </Paper>
          </Authorized>
        )}

        {loading && !previewData && (
          <Box sx={{ textAlign: 'center', p: 4 }}>
            <CircularProgress size={32} />
            <Typography variant="body2" sx={{ mt: 2, fontWeight: 600 }}>Processando documento...</Typography>
          </Box>
        )}

        {previewData && (
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="overline" sx={{ fontWeight: 800, color: 'text.secondary' }}>
                PRÉ-VISUALIZAÇÃO DA NOTA
              </Typography>
              <Button size="small" variant="outlined" color="error" onClick={removeFiles} startIcon={<X size={14} />}>
                Cancelar
              </Button>
            </Box>
            
            <FiscalDocumentPreview data={previewData} />

            <Authorized permission="supplychain.write">
              <Button 
                fullWidth 
                variant="contained" 
                size="large"
                color="success"
                onClick={confirmImport}
                disabled={loading}
                startIcon={loading ? <CircularProgress size={20} color="inherit" /> : <CheckCircle size={20} />}
                sx={{ mt: 4, py: 2, fontWeight: 900, borderRadius: 2 }}
              >
                {loading ? 'CONFIRMANDO...' : 'CONFIRMAR E IMPORTAR PARA ESTOQUE'}
              </Button>
            </Authorized>
          </Box>
        )}

        {files.length > 1 && !result && (
          <Box>
             <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
                ARQUIVOS EM LOTE ({files.length})
              </Typography>
              <Button size="small" onClick={removeFiles} startIcon={<X size={14} />} color="inherit">
                Limpar
              </Button>
            </Box>
            <Paper variant="outlined" sx={{ borderRadius: 2 }}>
               <List disablePadding sx={{ maxHeight: 300, overflow: 'auto' }}>
                {files.map((file, index) => (
                  <React.Fragment key={index}>
                    <ListItem>
                      <ListItemIcon sx={{ minWidth: 32 }}><FileText size={18} /></ListItemIcon>
                      <ListItemText 
                        primary={file.file.name} 
                        slotProps={{ primary: { variant: 'body2', sx: { fontWeight: 600 } } }} 
                      />
                    </ListItem>
                    {index < files.length - 1 && <Divider />}
                  </React.Fragment>
                ))}
              </List>
            </Paper>
            <Authorized permission="supplychain.write">
              <Button 
                fullWidth 
                variant="contained" 
                size="large"
                onClick={confirmImport}
                disabled={loading}
                sx={{ mt: 3, py: 1.5, fontWeight: 800, borderRadius: 2 }}
              >
                {loading ? 'Importando Lote...' : `Importar ${files.length} arquivos`}
              </Button>
            </Authorized>
          </Box>
        )}
        
        {result && (
          <Box sx={{ textAlign: 'center', p: 2 }}>
            <Alert severity="success" sx={{ mb: 3, fontWeight: 600 }}>{result}</Alert>
            <Button variant="outlined" size="large" onClick={removeFiles} sx={{ fontWeight: 700 }}>Importar outro</Button>
          </Box>
        )}
        
        {(error || batchErrors.length > 0) && (
          <Stack spacing={1}>
            {error && <Box sx={{ whiteSpace: 'pre-line', fontWeight: 600 }}><InlineError message={error} /></Box>}
            {batchErrors.map((err, i) => (
              <Alert key={i} severity="error" icon={<AlertCircle size={18} />} sx={{ '& .MuiAlert-message': { width: '100%' } }}>
                <Typography variant="caption" sx={{ fontWeight: 800, display: 'block' }}>{err.fileName}</Typography>
                <Typography variant="body2">{err.message}</Typography>
              </Alert>
            ))}
          </Stack>
        )}
      </Stack>
    </Box>
  );
}
