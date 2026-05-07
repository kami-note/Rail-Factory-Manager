namespace RailFactory.BuildingBlocks.Presentation;

/// <summary>
/// Encapsulates metadata for displaying system statuses in the UI.
/// This pattern follows the Backend-Driven UI (BFF) approach to ensure
/// consistent labeling and styling across all platforms.
/// </summary>
/// <param name="Key">The raw system identifier (e.g., "Approved").</param>
/// <param name="Label">The translated, human-readable label (e.g., "Conferido").</param>
/// <param name="Color">The semantic color for UI components (e.g., "success").</param>
public record DisplayStatus(string Key, string Label, string Color);
