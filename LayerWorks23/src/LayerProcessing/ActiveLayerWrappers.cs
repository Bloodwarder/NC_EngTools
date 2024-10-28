

using NameClassifiers;


namespace LayerWorks.LayerProcessing
{
    internal static class ActiveLayerWrappers
    {
        internal static List<LayerWrapper> List { get; } = new List<LayerWrapper>();

        internal static void Add(LayerWrapper lp)
        {
            List.Add(lp);
        }
        internal static void Push()
        {
            foreach (LayerWrapper lp in List) 
            { 
                lp.Push(); 
            }
        }
        internal static void Flush()
        {
            List.Clear();
        }
    }
}


