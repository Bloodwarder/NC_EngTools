

using NameClassifiers;


namespace LayerWorks.LayerProcessing
{
    internal static class ActiveLayerWrappers
    {
        internal static List<LayerWrapper> List { get; } = new List<LayerWrapper>();
        //internal static void StatusSwitch(Status status)
        //{
        //    foreach (LayerWrapper lp in List) { lp.StatusSwitch(status); };
        //}
        //internal static void Alter()
        //{
        //    foreach (LayerWrapper lp in List) { lp.Alter(); }
        //}
        //internal static void ReconstrSwitch()
        //{
        //    foreach (LayerWrapper lp in List) { lp.ReconstrSwitch(); }
        //}
        //internal static void ExtProjNameAssign(string extprojname)
        //{
        //    foreach (LayerWrapper lp in List) { lp.ExtProjNameAssign(extprojname); }
        //}
        internal static void Add(LayerWrapper lp)
        {
            List.Add(lp);
        }
        internal static void Push()
        {
            foreach (LayerWrapper lp in List) { lp.Push(); }
        }
        internal static void Flush()
        {
            List.Clear();
        }
    }
}


