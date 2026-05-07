export type StatusColor = 'success' | 'warning' | 'error' | 'info' | 'default';

/**
 * Standard contract for Backend-Driven UI statuses.
 * This matches the RailFactory.BuildingBlocks.Presentation.DisplayStatus C# record.
 * 
 * DESIGN DECISION: Single Source of Truth in Backend.
 * To avoid "Unknown/Desconhecido" errors, the Backend (BFF) is now responsible 
 * for providing the translated label and semantic color.
 */
export interface DisplayStatus {
  /** The raw system identifier (e.g., "Approved") */
  key: string;
  /** The translated, human-readable label (e.g., "Conferido") */
  label: string;
  /** The semantic color for UI components (e.g., "success") */
  color: StatusColor;
}
