import { createTheme, alpha } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    primary: { main: '#0078d4', contrastText: '#ffffff' },
    background: { default: '#f3f2f1', paper: '#ffffff' },
    text: { primary: '#201f1e', secondary: '#605e5c' },
    divider: '#edebe9',
  },
  typography: {
    fontFamily: '"Segoe UI", "Inter", -apple-system, sans-serif',
    h1: { fontSize: '1.5rem', fontWeight: 700, letterSpacing: '-0.02em' },
    h2: { fontSize: '1.25rem', fontWeight: 700, letterSpacing: '-0.015em' },
    h6: { fontSize: '0.85rem', fontWeight: 700, letterSpacing: '0.02em' },
    body1: { fontSize: '0.875rem', lineHeight: 1.5 },
    body2: { fontSize: '0.8125rem', lineHeight: 1.5 },
    caption: { fontSize: '0.7rem', fontWeight: 600, letterSpacing: '0.05em' },
  },
  shape: { borderRadius: 4 },
  spacing: 4,
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          WebkitFontSmoothing: 'antialiased',
          MozOsxFontSmoothing: 'grayscale',
          textRendering: 'optimizeLegibility',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: { 
          textTransform: 'none', 
          fontWeight: 600, 
          padding: '6px 16px',
          borderRadius: '4px',
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { 
          padding: '10px 16px', 
          borderColor: '#f3f2f1',
        },
        head: { 
          backgroundColor: '#faf9f8', 
          color: '#605e5c',
          fontWeight: 700, 
          textTransform: 'uppercase',
          fontSize: '0.65rem',
          borderBottom: '1px solid #edebe9',
        },
      },
    },
  },
});
