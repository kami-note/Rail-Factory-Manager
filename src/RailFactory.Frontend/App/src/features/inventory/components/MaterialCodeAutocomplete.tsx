import React, { useEffect, useRef, useState } from 'react';
import { Autocomplete, Box, CircularProgress, Stack, TextField, Typography } from '@mui/material';
import type { SxProps, Theme } from '@mui/material';
import { searchMaterials } from '../api/materials';
import type { MaterialSearchResult } from '../types';

interface MaterialCodeAutocompleteProps {
  tenantCode: string;
  value: string;
  onInputChange: (value: string) => void;
  onMaterialSelect?: (material: MaterialSearchResult) => void;
  label?: string;
  placeholder?: string;
  size?: 'small' | 'medium';
  fullWidth?: boolean;
  sx?: SxProps<Theme>;
  startAdornment?: React.ReactNode;
}

export function MaterialCodeAutocomplete({
  tenantCode,
  value,
  onInputChange,
  onMaterialSelect,
  label,
  placeholder,
  size = 'small',
  fullWidth,
  sx,
  startAdornment
}: MaterialCodeAutocompleteProps) {
  const [options, setOptions] = useState<MaterialSearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const [open, setOpen] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  const handleInputChange = (_: React.SyntheticEvent, inputValue: string) => {
    const upper = inputValue.toUpperCase();
    onInputChange(upper);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (upper.length < 2) { setOptions([]); return; }
    debounceRef.current = setTimeout(async () => {
      setSearching(true);
      try { setOptions(await searchMaterials(tenantCode, upper)); }
      catch { setOptions([]); }
      finally { setSearching(false); }
    }, 300);
  };

  return (
    <Autocomplete
      freeSolo
      fullWidth={fullWidth}
      sx={sx}
      options={options}
      getOptionLabel={o => typeof o === 'string' ? o : o.materialCode}
      filterOptions={x => x}
      loading={searching}
      open={open}
      onOpen={() => setOpen(true)}
      onClose={() => setOpen(false)}
      inputValue={value}
      onInputChange={handleInputChange}
      onChange={(_, selected) => {
        if (selected && typeof selected !== 'string') onMaterialSelect?.(selected);
      }}
      renderOption={(props, option) => (
        <Box component="li" {...props} key={option.materialCode}>
          <Stack>
            <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>
              {option.materialCode}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap>
              {option.officialName}
            </Typography>
          </Stack>
        </Box>
      )}
      renderInput={params => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const inputSlot = params.slotProps?.input as any;
        return (
          <TextField
            {...params}
            label={label}
            placeholder={placeholder}
            size={size}
            slotProps={{
              ...params.slotProps,
              input: {
                ...inputSlot,
                ...(startAdornment && { startAdornment }),
                endAdornment: (
                  <>
                    {searching && <CircularProgress size={14} />}
                    {inputSlot?.endAdornment}
                  </>
                ),
                style: { fontFamily: 'monospace', fontWeight: 700 }
              }
            }}
          />
        );
      }}
    />
  );
}
