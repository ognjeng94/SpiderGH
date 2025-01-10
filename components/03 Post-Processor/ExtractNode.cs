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
    public class ExtractNode : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractNode class.
        /// </summary>
        public ExtractNode()
          : base("Extract Node", "Extract ND",
              "Extract additional data from the selected Node",
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
            pManager.AddGenericParameter("Nodes'", "ND'", "Node data", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Node Index", "i", "Node index", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Node Index", "nd-i", "Node index", GH_ParamAccess.item);
            pManager.AddPointParameter("Initial node position", "nd-ptS", "Initial node position", GH_ParamAccess.item);
            pManager.AddPointParameter("Actual node position", "nd-ptE", "Actual node position", GH_ParamAccess.item);
            pManager.AddVectorParameter("Force", "nd-f", "Force", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mass", "nd-m", "Mass [kg]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Load", "nd-l", "Load [kg]", GH_ParamAccess.item);

            pManager.AddBooleanParameter("Anchor", "nd-a", "True if the node is anchor", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Move X", "nd-x", "True if the translation is allowed along the X-axis", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Move Y", "nd-y", "True if the translation is allowed along the Y-axis", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Move Z", "nd-z", "True if the translation is allowed along the Z-axis", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Node Neighbours", "nd-ngb", "Neighbour node index", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Node Neighbour Chains", "nd-chs", "Neighbour node's chain index", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //INPUT DATA
            List<S_Node> allNodes = new List<S_Node>();
            DA.GetDataList(0, allNodes);

            int index = 0;
            DA.GetData(1, ref index);

            //TEST DATA

            int counterNodes = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i].SimulationPhase < 1)
                {
                    counterNodes++;
                    allNodes.RemoveAt(i);

                    i -= 1;
                }

                if (allNodes.Count < 1)
                    break;
            }

            if (counterNodes > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some nodes were not simulated and were therefore deleted from the list");

            if (allNodes.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough nodes");
                return;
            }

            if (index < 0) index = 0;
            if (index > allNodes.Count - 1) index = allNodes.Count - 1;

            //SOLVER
            S_Node node = allNodes[index];

            int nodeIndex = node.Index;
            Point3d ptStart = node.InitialPosition;
            Point3d position = node.Position;
            Vector3d force = node.Force;
            double mass = node.Mass;
            double load = node.Load;

            bool isAnchor = node.IsAnchor;
            bool movex = node.MoveX;
            bool movey = node.MoveY;
            bool movez = node.MoveZ;

            List<int> neighbours = new List<int>();
            List<int> neighbourChains = new List<int>();

            for (int i = 0; i < node.Neighbours.Count; i++)
                neighbours.Add(node.Neighbours[i].Index);

            for (int i = 0; i < node.NeighbourChains.Count; i++)
                neighbourChains.Add(node.NeighbourChains[i].Index);

            //OUTPUT
            DA.SetData(0, nodeIndex);
            DA.SetData(1, ptStart);
            DA.SetData(2, position);
            DA.SetData(3, force);
            DA.SetData(4, mass);
            DA.SetData(5, load);

            DA.SetData(6, isAnchor);
            DA.SetData(7, movex);
            DA.SetData(8, movey);
            DA.SetData(9, movez);

            DA.SetDataList(10, neighbours);
            DA.SetDataList(11, neighbourChains);



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
                return SpiderGH.Properties.Resources._03_ExtractNode;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9BBFA1F5-CEF8-4A1E-8703-C45EE009CF98"); }
        }
    }
}