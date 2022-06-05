using System;
using LayerProcessing;


            string? str = null;
            do
            {
                str = null;
                str = Console.ReadLine();
                LayerParser lpr = new DebugLayerParser(str);
                Object o = new();
                lpr.ExtProjNameAssign("Аникеевка"); lpr.Push(o);
                lpr.ExtProjNameAssign("Опалиха"); lpr.Push(o);
                lpr.ExtProjNameAssign("Опалиха"); lpr.Push(o);
                lpr.ExtProjNameAssign(""); lpr.Push(o);
                lpr.ExtProjNameAssign(""); lpr.Push(o);
                lpr.StatusSwitch(LayerParser.Status.Existing); lpr.Push(o);
                lpr.ReconstrSwitch(); lpr.Push(o);
                lpr.StatusSwitch(LayerParser.Status.Planned); lpr.Push(o);
                lpr.ReconstrSwitch(); lpr.Push(o);
                lpr.StatusSwitch(LayerParser.Status.NSPlanned); lpr.Push(o);

            } while (str!=null);  