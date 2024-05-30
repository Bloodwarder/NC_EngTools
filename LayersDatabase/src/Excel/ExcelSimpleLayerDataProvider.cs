using NPOI.SS.UserModel;

namespace LayersIO.Excel
{
    public class ExcelSimpleLayerDataProvider<TKey, TValue> : ExcelLayerDataProvider<TKey, TValue> where TKey : class where TValue : class
    {
        public ExcelSimpleLayerDataProvider(string path, string sheetname) : base(path, sheetname)
        {
        }
        private protected override TValue CellsExtract(ICell rng)
        {
            return (TValue)_valueHandler[typeof(TValue)](rng);
        }
    }
}