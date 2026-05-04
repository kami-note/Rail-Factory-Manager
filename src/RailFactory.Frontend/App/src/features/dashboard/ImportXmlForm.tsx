import React, { useMemo, useState } from 'react';
import { 
  Box, 
  Typography, 
  Button, 
  Stack, 
  TextField, 
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
  name: string;
  size: number;
  content: string;
};

function readXmlFile(file: File): Promise<string> {
  if (typeof file.text === 'function') {
    return file.text();
  }

  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ''));
    reader.onerror = () => reject(reader.error ?? new Error(`Could not read ${file.name}.`));
    reader.readAsText(file);
  });
}

export function ImportXmlForm({ tenantCode, showTitle = true }: ImportXmlFormProps) {
  const sampleXml = useMemo(
    () => `<receipt>\n  <receiptNumber>RCPT-XML-001</receiptNumber>\n  <documentNumber>DOC-XML-001</documentNumber>\n  <receiptDate>${new Date().toISOString().slice(0, 10)}</receiptDate>\n  <supplier>\n    <fiscalId>99887766000100</fiscalId>\n    <name>XML Supplier</name>\n  </supplier>\n  <items>\n    <item>\n      <materialCode>MAT-XML-001</materialCode>\n      <quantity>5</quantity>\n      <uom>UN</uom>\n    </item>\n  </items>\n</receipt>`,
    []
  );

  const [xml, setXml] = useState(sampleXml);
  const [files, setFiles] = useState<SelectedXmlFile[]>([]);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const importXml = async (xmlContent: string) => {
    const response = await fetch('/api/supply-chain/receipts/import/xml', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'X-Tenant-Code': tenantCode
      },
      body: JSON.stringify({ xmlContent })
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
      throw new Error(problem?.detail ?? problem?.title ?? `XML import failed: ${response.status}`);
    }

    return (await response.json()) as { receiptId?: string; id?: string };
  };

  const importXmlBatch = async (documents: SelectedXmlFile[]) => {
    const response = await fetch('/api/supply-chain/receipts/import/xml/batch', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'X-Tenant-Code': tenantCode
      },
      body: JSON.stringify({
        documents: documents.map(file => ({ fileName: file.name, xmlContent: file.content }))
      })
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

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files ?? []);
    if (selectedFiles.length === 0) return;
    
    setError(null);
    setResult(null);

    try {
      const loadedFiles = await Promise.all(
        selectedFiles.map(async file => ({
          name: file.name,
          size: file.size,
          content: await readXmlFile(file)
        }))
      );
      setFiles(loadedFiles);
      if (loadedFiles.length === 1) {
        setXml(loadedFiles[0].content);
      }
    } catch (fileError) {
      setFiles([]);
      setError(fileError instanceof Error ? fileError.message : 'Could not read XML file.');
    }
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

      const xmlContent = files.length === 1 ? files[0].content : xml;
      const data = await importXml(xmlContent);
      setResult(`XML imported. Receipt: ${data.receiptId ?? data.id}`);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unexpected XML import error.');
    } finally {
      setLoading(false);
    }
  };

  const removeFiles = () => {
    setFiles([]);
    setXml(sampleXml);
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
                  <React.Fragment key={`${file.name}-${index}`}>
                    <ListItem>
                      <ListItemIcon sx={{ minWidth: 32 }}>
                        <FileText size={18} />
                      </ListItemIcon>
                      <ListItemText 
                        primary={file.name} 
                        secondary={`${Math.max(1, Math.round(file.size / 1024))} KB`}
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

        {files.length <= 1 && (
          <TextField
            fullWidth
            multiline
            rows={10}
            label="XML Content"
            variant="outlined"
            value={xml}
            onChange={e => setXml(e.target.value)}
            slotProps={{
              input: {
                sx: { fontFamily: 'monospace', fontSize: '0.8rem' }
              }
            }}
          />
        )}

        <Stack spacing={2}>
          <Button 
            type="submit" 
            variant="contained" 
            disabled={loading || (files.length === 0 && !xml)}
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
