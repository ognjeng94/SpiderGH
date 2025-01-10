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
    public class ExtractChain : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractChain class.
        /// </summary>
        public ExtractChain()
          : base("Extract Chain", "Extract CH",
              "Extract additional data from the selected Chain",
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
            pManager.AddIntegerParameter("Chain Index", "i", "Chain index", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Chain Index", "ch-i", "Chain index", GH_ParamAccess.item);
            pManager.AddLineParameter("Initial Line", "ch-ln", "Initial chain line", GH_ParamAccess.item);
            pManager.AddCurveParameter("Chain polyline", "ch-pl", "Chain polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Elasticity", "ch-k", "Elasticity [0.01-10]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mass/m", "ch-m", "Mass [kg] per meter", GH_ParamAccess.item);
            pManager.AddNumberParameter("Node-Resolution", "ch-nr", "The average distance of the nodes on the initial line. Smaller number = more nodes", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Node Indices", "nd-i", "Node indices of the selected chain", GH_ParamAccess.list);
            pManager.AddPointParameter("Nodes", "nd-pt", "Nodes of the selected chain", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Anchors", "nd-a", "True if the particular node of the selected chain is anchor", GH_ParamAccess.list);

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

            int index = 0;
            DA.GetData(1, ref index);

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

            if (index < 0) index = 0;
            if (index > allChains.Count - 1) index = allChains.Count - 1;

            //SOLVER
            S_Chain chain = allChains[index];

            int chainIndex = chain.Index;
            Line chainLine = chain.InitialLineCurve.Line;
            Polyline polyline = chain.GeneratePolyline();
            double elasticity = chain.Elasticity;
            double massPerM = chain.MassPerM;
            double nodeRes = chain.NodeResolution;

            List<int> nodeIndices = new List<int>();
            Point3dList nodes = new Point3dList();
            List<bool> isNodeAnchor = new List<bool>();

            for (int i = 0; i < chain.Nodes.Count; i++)
            {
                nodeIndices.Add(chain.Nodes[i].Index);
                nodes.Add(chain.Nodes[i].Position);
                isNodeAnchor.Add(chain.Nodes[i].IsAnchor);
            }

            //OUTPUT
            DA.SetData(0, chainIndex);
            DA.SetData(1, chainLine);
            DA.SetData(2, polyline);
            DA.SetData(3, elasticity);
            DA.SetData(4, massPerM);
            DA.SetData(5, nodeRes);

            DA.SetDataList(6, nodeIndices);
            DA.SetDataList(7, nodes);
            DA.SetDataList(8, isNodeAnchor);

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
                return SpiderGH.Properties.Resources._03_ExtractChain;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CB3D40CA-6ADF-4563-8803-E39DE3DB9A33"); }
        }
    }
}