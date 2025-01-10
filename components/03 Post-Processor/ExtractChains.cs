using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Rhino;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;
using Rhino.Geometry.Intersect;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SpiderGH.components;

namespace SpiderGH
{
    public class ExtractChains : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractChains class.
        /// </summary>
        public ExtractChains()
          : base("Extract Chains", "Extract CHs",
              "Extract additional data from the Chains",
              Universal.Category(), Universal.SubCategory_PostProcessor())
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chains'", "CH'", "Chain data ", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Chain Indices", "chs-i", "Chain index", GH_ParamAccess.list);
            pManager.AddLineParameter("Initial Lines", "chs-ln", "Initial chain line", GH_ParamAccess.list);
            pManager.AddCurveParameter("Chain polylines", "chs-pl", "Chain polyline", GH_ParamAccess.list);
            pManager.AddNumberParameter("Elasticity", "chs-k", "Elasticity [0.01-10]", GH_ParamAccess.list);
            pManager.AddNumberParameter("Mass/m", "chs-m", "Mass [kg] per meter", GH_ParamAccess.list);
            pManager.AddNumberParameter("Node-Resolutions", "chs-nr", "The average distance of the nodes on the initial line. Smaller number = more nodes", GH_ParamAccess.list);

            pManager.AddTextParameter("Node Indices", "nds-i", "Node indices of the selected chain", GH_ParamAccess.list);
            pManager.AddTextParameter("Anchors", "nds-a", "1 = anchor, 0 = regular node", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //INPUT DATA
            List<S_Chain> allChains = new List<S_Chain>();
            DA.GetDataList(0, allChains);

            //TEST DATA

            int counterChains = 0;
            for (int i = 0; i < allChains.Count; i++)
            {
                if (allChains[i].SimulationPhase < 1)
                {
                    counterChains++;
                    allChains.RemoveAt(i);

                    i -= 1;
                }

                if (allChains.Count < 1)
                    break;
            }

            if (counterChains > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some chains were not simulated and were therefore deleted from the list");

            if (allChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough chains");
                return;
            }

            //SOLVER
            List<int> chainIndices = new List<int>();
            List<Line> chainLines = new List<Line>();
            List<Polyline> chainPolylines = new List<Polyline>();
            List<double> chainElasticity = new List<double>();
            List<double> chainMassPerM = new List<double>();
            List<double> chainNodeRes = new List<double>();

            List<string> nodeIndices = new List<string>();
            List<string> nodeAnchor = new List<string>();

            for (int i = 0; i < allChains.Count; i++)
            {
                S_Chain chain = allChains[i];

                chainIndices.Add(chain.Index);
                chainLines.Add(chain.InitialLineCurve.Line);
                chainPolylines.Add(chain.GeneratePolyline());
                chainElasticity.Add(chain.Elasticity);
                chainMassPerM.Add(chain.MassPerM);
                chainNodeRes.Add(chain.NodeResolution);


                string indices = "";
                for (int k = 0; k < chain.Nodes.Count; k++)
                {
                    if (k != chain.Nodes.Count - 1)
                    {
                        indices += chain.Nodes[k].Index.ToString() + "_";
                    }
                    else
                    {
                        indices += chain.Nodes[k].Index.ToString();
                    }
                }
                nodeIndices.Add(indices);

                string anchors = "";
                for (int k = 0; k < chain.Nodes.Count; k++)
                {
                    int anchor = -1;
                    if (chain.Nodes[k].IsAnchor == true)
                        anchor = 1;
                    else
                        anchor = 0;

                    if (k != chain.Nodes.Count - 1)
                    {
                        anchors += anchor.ToString() + "_";
                    }
                    else
                    {
                        anchors += anchor.ToString();
                    }
                }
                nodeAnchor.Add(anchors);

            }

            //OUTPUT
            DA.SetDataList(0, chainIndices);
            DA.SetDataList(1, chainLines);
            DA.SetDataList(2, chainPolylines);
            DA.SetDataList(3, chainElasticity);
            DA.SetDataList(4, chainMassPerM);
            DA.SetDataList(5, chainNodeRes);

            DA.SetDataList(6, nodeIndices);
            DA.SetDataList(7, nodeAnchor);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return SpiderGH.Properties.Resources._03_ExtractChains;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("62EDED93-D9E4-4A84-AF0E-0ED8020549A6"); }
        }
    }
}