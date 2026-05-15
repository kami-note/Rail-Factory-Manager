import { Box } from '@mui/material';
import { InlineError } from './InlineError';

type PageErrorProps = {
  message: string;
};

export function PageError({ message }: PageErrorProps) {
  return (
    <Box sx={{ p: 4 }}>
      <InlineError message={message} />
    </Box>
  );
}
