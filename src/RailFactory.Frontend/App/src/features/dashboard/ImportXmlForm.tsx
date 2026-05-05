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
import { FileText, UploadCloud, X } from 'lucide-react';

type ImportXmlFormProps = {
  tenantCode: string;
  showTitle?: boolean;
};

type SelectedXmlFile = {
  file: File;
};

export function ImportXmlForm({ tenantCode, showTitle = true }: ImportXmlFormProps) {
  const [files, setFiles] = useState<SelectedXmlFile[]>([]);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const importXml = async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/supply-chain/receipts/import/xml', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'X-Tenant-Code': tenantCode
      },
      body: formData
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
      throw new Error(problem?.detail ?? problem?.title ?? `XML import failed: ${response.status}`);
    }

    return (await response.json()) as { receiptId?: string; id?: string };
  };

  const importXmlBatch = async (documents: SelectedXmlFile[]) => {
    const formData = new FormData();
    for (const document of documents) {
      formData.append('files', document.file);
    }

    const response = await fetch('/api/supply-chain/receipts/import/xml/batch', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'X-Tenant-Code': tenantCode
      },
      body: formData
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null) as {
        detail?: string;
        title?: string;
        errors?: { fileName?: string; message?: string }[];
      } | null;
      const batchErrors = problem?.errors
        ?.map(item => `${item.fileName ?? 'XML'}: ${item.message ?? 'Invalid XML.'}`)
        .join('\n');

      throw new Error(batchErrors ?? problem?.detail ?? problem?.title ?? `XML batch import failed: ${response.status}`);
    }

    return (await response.json()) as {
      imported?: { fileName: string; receiptId: string; receiptNumber: string; documentNumber: string }[];
    };
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files ?? []);
    if (selectedFiles.length === 0) return;
    
    setError(null);
    setResult(null);
    setFiles(selectedFiles.map(file => ({ file })));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setResult(null);
    setError(null);
    setLoading(true);

    try {
      if (files.length > 1) {
        const data = await importXmlBatch(files);
        const imported = data.imported ?? [];
        setResult(`${imported.length} XML files imported. Receipts: ${imported.map(x => x.receiptNumber).join(', ')}`);
        return;
      }

      const selectedFile = files[0]?.file;
      if (!selectedFile) {
        throw new Error('An XML file is required.');
      }

      const data = await importXml(selectedFile);
      setResult(`XML imported. Receipt: ${data.receiptId ?? data.id}`);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unexpected XML import error.');
    } finally {
      setLoading(false);
    }
  };

  const removeFiles = () => {
    setFiles([]);
    setResult(null);
    setError(null);
  };

  return (
    <Box component="form" onSubmit={handleSubmit}>
      {showTitle && (
        <Typography variant="h6" sx={{ mb: 3, fontWeight: 700 }}>
          Import receipt XML
        </Typography>
      )}

      <Stack spacing={3}>
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

        {files.length > 0 && (
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                SELECTED FILES ({files.length})
              </Typography>
              <Button size="small" onClick={removeFiles} startIcon={<X size={14} />}>
                Clear
              </Button>
            </Box>
            <Paper variant="outlined">
              <List disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
                {files.map((file, index) => (
                  <React.Fragment key={`${file.file.name}-${index}`}>
                    <ListItem>
                      <ListItemIcon sx={{ minWidth: 32 }}>
                        <FileText size={18} />
                      </ListItemIcon>
                      <ListItemText 
                        primary={file.file.name} 
                        secondary={`${Math.max(1, Math.round(file.file.size / 1024))} KB`}
                        slotProps={{
                          primary: { variant: 'body2', fontWeight: 600 },
                          secondary: { variant: 'caption' }
                        }}
                      />
                    </ListItem>
                    {index < files.length - 1 && <Divider />}
                  </React.Fragment>
                ))}
              </List>
            </Paper>
          </Box>
        )}

        <Stack spacing={2}>
          <Button 
            type="submit" 
            variant="contained" 
            disabled={loading || files.length === 0}
            startIcon={loading ? <CircularProgress size={20} /> : null}
          >
            {loading ? 'Importing...' : files.length > 1 ? `Import ${files.length} files` : 'Import XML'}
          </Button>
          
          {result && <Alert severity="success">{result}</Alert>}
          {error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{error}</Alert>}
        </Stack>
      </Stack>
    </Box>
  );
}
