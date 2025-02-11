using NameClassifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabaseEditor.ViewModel
{
    public record SimpleLayer (string Prefix, string MainName, string? Status) 
    {
        public string Name =>
            Status != null ?
            string.Join(NameParser.LoadedParsers[Prefix].Separator, Prefix, MainName) :
            string.Join(NameParser.LoadedParsers[Prefix].Separator, Prefix, MainName, Status);
    }
}
