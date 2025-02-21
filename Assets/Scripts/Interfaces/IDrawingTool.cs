public interface IDrawingTool
{
    void OnToolUpdate();
    void OnToolSelected();
    void OnToolDeselected();
    bool CanUse();
} 