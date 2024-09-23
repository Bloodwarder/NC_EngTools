using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoaderCore.Integrity
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NcetModuleInitializerAttribute : Attribute
    {
    }
}
