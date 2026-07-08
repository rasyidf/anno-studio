namespace AnnoDesigner.Core.Models
{
    /// <summary>
    /// Alignment options for layout operations.
    /// </summary>
    public enum AlignmentMode
    {
        Left,
        Center,
        Right,
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// Distribution modes for layout operations.
    /// </summary>
    public enum DistributionMode
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Z-order actions.
    /// </summary>
    public enum ZOrderAction
    {
        BringToFront,
        SendToBack,
        BringForward,
        SendBackward
    }

    /// <summary>
    /// Rotation directions used by the rotate command.
    /// </summary>
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// Flip directions used by the flip command.
    /// </summary>
    public enum FlipDirection
    {
        Horizontal,
        Vertical
    }
}
