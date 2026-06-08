import React from 'react';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ConfigureIntegrationModal } from '../components/ConfigureIntegrationModal';
import { ThemeProvider, createTheme } from '@mui/material';

// Mock api
vi.mock('../api/integrations', () => ({
  configureIntegration: vi.fn(),
}));

const theme = createTheme();

function renderWithTheme(component: JSX.Element) {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
}

describe('ConfigureIntegrationModal', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('renders provider select option and header', () => {
    renderWithTheme(
      <ConfigureIntegrationModal
        open={true}
        tenantCode="dev-tenant"
        category="fiscal"
        existingProviderType=""
        onSaved={vi.fn()}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText(/Configurar — Fiscal \(NF-e\)/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Selecione o Provedor/i)[0]).toBeInTheDocument();
  });

  it('renders fields when provider is pre-selected', () => {
    renderWithTheme(
      <ConfigureIntegrationModal
        open={true}
        tenantCode="dev-tenant"
        category="fiscal"
        existingProviderType="plugnotas"
        onSaved={vi.fn()}
        onClose={vi.fn()}
      />
    );

    // Should show API Key field under Credenciais tab by default
    expect(screen.getByLabelText(/API Key/i)).toBeInTheDocument();
  });
});
