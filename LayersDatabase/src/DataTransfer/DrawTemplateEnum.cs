namespace LayersIO.DataTransfer
{
    [Flags]
    public enum DrawTemplate
    {
        Undefined = 0,
        BlockReference          = 0b0000000001,
        DoubleSolidLine         = 0b0000000010,
        FencedRectangle         = 0b0000000100,
        HatchedCircle           = 0b0000001000,
        HatchedRectangle        = 0b0000010000,
        HatchedFencedRectangle  = 0b0000100000,
        MarkedDashedLine        = 0b0001000000,
        MarkedSolidLine         = 0b0010000000,
        SolidLine               = 0b0100000000,
        Rectangle               = 0b1000000000
    }
}