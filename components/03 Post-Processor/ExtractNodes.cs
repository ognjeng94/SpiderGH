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
    public class ExtractNodes : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractNodes class.
        /// </summary>
        public ExtractNodes()
          : base("Extract Nodes", "Extract NDs",
              "Extract additional data from the Nodes",
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

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Node Indices", "nds-i", "Node indices", GH_ParamAccess.list);
            pManager.AddPointParameter("Initial node positions", "nds-ptS", "Initial node positions", GH_ParamAccess.list);
            pManager.AddPointParameter("Actual node positions", "nds-ptE", "Actual node positions", GH_ParamAccess.list);
            pManager.AddVectorParameter("Forces", "nds-f", "Forces", GH_ParamAccess.list);
            pManager.AddNumberParameter("Mass", "nds-m", "Mass [kg]", GH_ParamAccess.list);
            pManager.AddNumberParameter("Loads", "nds-l", "Loads [kg]", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Anchors", "nds-a", "True if the node is anchor", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Move X", "nds-x", "True if the translation is allowed along the X-axis", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Move Y", "nds-y", "True if the translation is allowed along the Y-axis", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Move Z", "nds-z", "True if the translation is allowed along the Z-axis", GH_ParamAccess.list);

            pManager.AddTextParameter("Node Neighbours", "nds-ngb", "Neighbour node index", GH_ParamAccess.list);
            pManager.AddTextParameter("Node Neighbour Chains", "nds-chs", "Neighbour node's chain index", GH_ParamAccess.list);


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

            //SOLVER

            List<int> nodeIndices = new List<int>();
            Point3dList ptStarts = new Point3dList();
            Point3dList positions = new Point3dList();
            List<Vector3d> forces = new List<Vector3d>();
            List<double> mass = new List<double>();
            List<double> load = new List<double>();

            List<bool> isAnchor = new List<bool>();
            List<bool> movex = new List<bool>();
            List<bool> movey = new List<bool>();
            List<bool> movez = new List<bool>();

            List<string> neighbours = new List<string>();
            List<string> neighbourChains = new List<string>();

            for (int i = 0; i < allNodes.Count; i++)
            {
                S_Node node = allNodes[i];

                nodeIndices.Add(node.Index);
                ptStarts.Add(node.InitialPosition);
                positions.Add(node.Position);
                forces.Add(node.Force);
                mass.Add(node.Mass);
                load.Add(node.Load);

                isAnchor.Add(node.IsAnchor);
                movex.Add(node.MoveX);
                movey.Add(node.MoveY);
                movez.Add(node.MoveZ);


                string indices = "";
                for (int k = 0; k < node.Neighbours.Count; k++)
                {
                    if (k != node.Neighbours.Count - 1)
                    {
                        indices += node.Neighbours[k].Index.ToString() + "_";
                    }
                    else
                    {
                        indices += node.Neighbours[k].Index.ToString();
                    }
                }
                neighbours.Add(indices);

                string chains = "";
                for (int k = 0; k < node.NeighbourChains.Count; k++)
                {
                    if (k != node.NeighbourChains.Count - 1)
                    {
                        chains += node.NeighbourChains[k].Index.ToString() + "_";
                    }
                    else
                    {
                        chains += node.NeighbourChains[k].Index.ToString();
                    }
                }
                neighbourChains.Add(chains);

            }


            //OUTPUT
            DA.SetDataList(0, nodeIndices);
            DA.SetDataList(1, ptStarts);
            DA.SetDataList(2, positions);
            DA.SetDataList(3, forces);
            DA.SetDataList(4, mass);
            DA.SetDataList(5, load);

            DA.SetDataList(6, isAnchor);
            DA.SetDataList(7, movex);
            DA.SetDataList(8, movey);
            DA.SetDataList(9, movez);

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
                return SpiderGH.Properties.Resources._03_ExtractNodes;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C7B17B5A-ADDB-45F5-9869-D680C846A140"); }
        }
    }
}