import { cleanup, fireEvent, render, screen, waitFor, act } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { MergeMaterialModal } from '../components/MergeMaterialModal';
import { ThemeProvider, createTheme } from '@mui/material';
import { searchMaterials, mergeMaterials } from '../api/materials';

// Mock the API module
vi.mock('../api/materials');

const theme = createTheme();

function renderWithTheme(component: JSX.Element) {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
}

describe('MergeMaterialModal', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('searches for materials and confirms merge', async () => {
    vi.mocked(searchMaterials).mockResolvedValue([
      {
        materialCode: 'OFFICIAL-01',
        officialName: 'Material Oficial',
        description: 'Desc',
        category: 'RawMaterial'
      }
    ]);
    vi.mocked(mergeMaterials).mockResolvedValue(undefined);

    const onSuccess = vi.fn();
    const onClose = vi.fn();

    renderWithTheme(
      <MergeMaterialModal
        open={true}
        onClose={onClose}
        onSuccess={onSuccess}
        tenantCode="dev"
        obsoleteMaterialCode="OLD-01"
        obsoleteMaterialName="Material Antigo"
      />
    );

    const searchInput = screen.getByPlaceholderText(/Digite código, nome ou GTIN/i);
    
    await act(async () => {
      fireEvent.focus(searchInput);
      fireEvent.change(searchInput, { target: { value: 'OFFICIAL' } });
      // We need to wait for the debounce in real time
      await new Promise(resolve => setTimeout(resolve, 600));
    });

    // Wait for the mock to be called
    await waitFor(() => expect(searchMaterials).toHaveBeenCalledWith('dev', 'OFFICIAL'), { timeout: 2000 });

    // Select the option
    const option = await screen.findByText(/OFFICIAL-01 - Material Oficial/i, {}, { timeout: 2000 });
    fireEvent.click(option);

    const confirmButton = screen.getByRole('button', { name: /Confirmar Unificação/i });
    await act(async () => {
      fireEvent.click(confirmButton);
    });

    await waitFor(() => expect(mergeMaterials).toHaveBeenCalledWith('dev', 'OLD-01', 'OFFICIAL-01'));
    expect(onSuccess).toHaveBeenCalledWith('OFFICIAL-01');
  });

  it('shows error message when merge fails', async () => {
    vi.mocked(searchMaterials).mockResolvedValue([{ materialCode: 'OFF-1', officialName: 'Off' }]);
    vi.mocked(mergeMaterials).mockRejectedValue(new Error('Erro ao unificar'));

    renderWithTheme(
      <MergeMaterialModal
        open={true}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
        tenantCode="dev"
        obsoleteMaterialCode="OLD-1"
        obsoleteMaterialName="Old"
      />
    );

    const searchInput = screen.getByPlaceholderText(/Digite código, nome ou GTIN/i);
    
    await act(async () => {
      fireEvent.focus(searchInput);
      fireEvent.change(searchInput, { target: { value: 'OFF' } });
      await new Promise(resolve => setTimeout(resolve, 600));
    });

    const option = await screen.findByText(/OFF-1 - Off/i, {}, { timeout: 2000 });
    fireEvent.click(option);

    const confirmButton = screen.getByRole('button', { name: /Confirmar Unificação/i });
    await act(async () => {
      fireEvent.click(confirmButton);
    });

    expect(await screen.findByText(/Erro ao unificar/i)).toBeInTheDocument();
  });
});
