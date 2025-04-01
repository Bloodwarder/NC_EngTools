namespace LayersIO.DataTransfer
{
    [Flags]
    public enum DrawTemplate
    {
        Undefined = 0,
        BlockReference          = 1,
        DoubleSolidLine         = 2,
        FencedRectangle         = 4,
        HatchedCircle           = 8,
        HatchedRectangle        = 16,
        HatchedFencedRectangle  = 32,
        MarkedDashedLine        = 64,
        MarkedSolidLine         = 128,
        SolidLine               = 256,
        Rectangle               = 512
    }
}