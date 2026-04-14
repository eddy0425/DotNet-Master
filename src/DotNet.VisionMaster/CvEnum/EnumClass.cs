namespace DotNet.VisionMaster
{
    /// <summary> 设置步骤枚举 </summary>
    public enum SetUpEnum
    {
        None,
        Step1,
        Step2,
        Step3,
        Step4,
        Step5
    }

    /// <summary> 循环移动状态枚举 </summary>
    public enum CycleMoveEnum
    {
        None,
        Start,
        StartMove,
        End,
        EndMove,
        Center,
        CenterMove
    }

    /// <summary> 绘画类型枚举 </summary>
    public enum DrawEnum
    {
        None,
        SetModel,
        NewRect,
        EditRect,
        DispRect,
        NewPolygon,
        EditPolygon,
        Synthethic,
        ShapeModel
    }

}
