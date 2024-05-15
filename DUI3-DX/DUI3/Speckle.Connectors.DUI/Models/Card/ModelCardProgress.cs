namespace Speckle.Connectors.DUI.Models.Card;

/// <summary>
/// Progress value between 0 and 1 to calculate UI progress bar width.
/// If it is null it will swooshing on UI.
/// </summary>
public record ModelCardProgress(string ModelCardId, string Status, double? Progress);
