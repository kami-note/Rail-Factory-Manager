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
  Divider
} from '@mui/material';
import { FileText, UploadCloud, X, CheckCircle, AlertCircle } from 'lucide-react';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../lib/http';
import { FiscalDocumentPreview, ParsedReceiptDocument } from './FiscalDocumentPreview';

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

export function ImportXmlForm({ tenantCode, showTitle = true }: ImportXmlFormProps) {
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
        'Failed to parse XML for preview'
      );
      setPreviewData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error previewing XML.');
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
          'XML import failed'
        );
        setResult(`XML imported successfully. Receipt ID: ${data.receiptId ?? data.id}`);
        setPreviewData(null);
        setFiles([]);
      } else {
        const formData = new FormData();
        for (const f of files) {
          formData.append('files', f.file);
        }

        const response = await fetch('/api/supply-chain/receipts/import/xml/batch', {
          method: 'POST',
          credentials: 'include',
          headers: buildTenantHeaders(tenantCode),
          body: formData
        });

        const data = await response.json().catch(() => null);

        if (!response.ok) {
          if (data?.errors) {
            setBatchErrors(data.errors);
            throw new Error('Batch import failed with validation errors.');
          }
          throw new Error(data?.detail ?? 'Batch import failed');
        }

        setResult(`${data.imported?.length ?? 0} files imported in batch.`);
        setFiles([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error importing XML.');
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
        <Typography variant="h6" sx={{ mb: 3, fontWeight: 700 }}>
          Import receipt XML
        </Typography>
      )}

      <Stack spacing={3}>
        {!previewData && !result && (
          <Paper
            variant="outlined"
            sx={{
              p: 3,
              textAlign: 'center',
              cursor: 'pointer',
              borderStyle: 'dashed',
              borderWidth: 2,
              bgcolor: 'background.default',
              '&:hover': {
                borderColor: 'primary.main',
                bgcolor: 'action.hover'
              },
              position: 'relative'
            }}
            component="label"
          >
            <input
              type="file"
              accept=".xml,text/xml,application/xml"
              multiple
              hidden
              onChange={handleFileChange}
            />
            <UploadCloud size={32} color="currentColor" />
            <Typography variant="body1" sx={{ fontWeight: 600, mt: 1 }}>
              Upload XML files
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Single invoice or batch import
            </Typography>
          </Paper>
        )}

        {loading && !previewData && (
          <Box sx={{ textAlign: 'center', p: 4 }}>
            <CircularProgress size={32} />
            <Typography variant="body2" sx={{ mt: 1 }}>Processing document...</Typography>
          </Box>
        )}

        {previewData && (
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                PREVIEW
              </Typography>
              <Button size="small" variant="outlined" color="error" onClick={removeFiles} startIcon={<X size={14} />}>
                Cancel
              </Button>
            </Box>
            
            <FiscalDocumentPreview data={previewData} />

            <Button 
              fullWidth 
              variant="contained" 
              size="large"
              color="success"
              onClick={confirmImport}
              disabled={loading}
              startIcon={loading ? <CircularProgress size={20} color="inherit" /> : <CheckCircle size={20} />}
              sx={{ mt: 4, py: 1.5, fontWeight: 900 }}
            >
              {loading ? 'CONFIRMING...' : 'CONFIRM AND IMPORT TO INVENTORY'}
            </Button>
          </Box>
        )}

        {files.length > 1 && !result && (
          <Box>
             <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                BATCH FILES ({files.length})
              </Typography>
              <Button size="small" onClick={removeFiles} startIcon={<X size={14} />}>
                Clear
              </Button>
            </Box>
            <Paper variant="outlined">
               <List disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
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
            <Button 
              fullWidth 
              variant="contained" 
              onClick={confirmImport}
              disabled={loading}
              sx={{ mt: 2 }}
            >
              {loading ? 'Importing Batch...' : `Import ${files.length} files`}
            </Button>
          </Box>
        )}
        
        {result && (
          <Box sx={{ textAlign: 'center', p: 2 }}>
            <Alert severity="success" sx={{ mb: 3 }}>{result}</Alert>
            <Button variant="outlined" onClick={removeFiles}>Import another</Button>
          </Box>
        )}
        
        {(error || batchErrors.length > 0) && (
          <Stack spacing={1}>
            {error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{error}</Alert>}
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
