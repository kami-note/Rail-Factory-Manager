import React from 'react';
import { Avatar, Tooltip } from '@mui/material';
import { Package } from 'lucide-react';

/**
 * Props for the MaterialAvatar component.
 */
interface MaterialAvatarProps {
  /** The unique code for the material/product. */
  materialCode: string;
  /** The human-readable description or name of the material. */
  description?: string;
  /** Optional size for the avatar (standard MUI Avatar sizes or numeric). */
  size?: number | 'small' | 'medium' | 'large';
  /** Optional image URL for real material/product photo. */
  imageUrl?: string;
}

/**
 * Renders a deterministic visual identifier for a material or product.
 * @param materialCode - The material SKU/Code.
 * @param description - Optional description for tooltips and accessibility.
 * @param size - Visual size of the avatar.
 * @remarks
 * This component generates a consistent background color based on the material code hash,
 * ensuring that the same product always has the same visual representation across the app.
 * It uses Lucide "Package" icon as a default representation.
 */
export const MaterialAvatar: React.FC<MaterialAvatarProps> = ({ 
  materialCode, 
  description, 
  size = 40,
  imageUrl
}) => {
  // Simple deterministic hash to pick a color
  const getHashColor = (str: string) => {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash % 360);
    return `hsl(${hue}, 45%, 45%)`;
  };

  const backgroundColor = getHashColor(materialCode);
  const avatarSize = typeof size === 'number' ? size : (size === 'small' ? 32 : size === 'large' ? 56 : 40);

  const avatar = (
    <Avatar
      variant="rounded"
      src={imageUrl}
      sx={{ 
        bgcolor: backgroundColor, 
        width: avatarSize, 
        height: avatarSize,
        fontSize: avatarSize * 0.5,
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
      }}
      aria-label={`Visual identifier for ${description || materialCode}`}
    >
      <Package size={avatarSize * 0.6} color="white" strokeWidth={1.5} />
    </Avatar>
  );

  if (description) {
    return (
      <Tooltip title={`${description} (${materialCode})`} arrow placement="top">
        {avatar}
      </Tooltip>
    );
  }

  return avatar;
};
